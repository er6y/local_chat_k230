# board: file-backed flush recorder (no runtime code relocation, icache-safe path)
import ctypes
import mmap
import os
import struct
import time

import numpy as np
import nncaseruntime as nn

BUF_VA = 0x600000000  # fixed anonymous RW mapping

base = None
for ln in open('/proc/self/maps'):
    if '_nncaseruntime_k230' in ln and 'r-xp' in ln:
        base = int(ln.split('-')[0], 16)
        break
assert base, 'so base not found'
GOT_OFF = 0x4bd010
ORIG_OFF = 0x3fae00
got = base + GOT_OFF
orig = base + ORIG_OFF
print('base=0x%x got=0x%x orig=0x%x' % (base, got, orig))

# --- build stub bytes (offline constants only) ---


def w(*words):
    return b''.join(struct.pack('<I', x) for x in words)


def lui(rd, imm):
    return (imm << 12) | (rd << 7) | 0x37


def addi(rd, rs1, imm):
    return ((imm & 0xFFF) << 20) | (rs1 << 15) | (rd << 7) | 0x13


def lw(rd, rs1, imm):
    return ((imm & 0xFFF) << 20) | (rs1 << 15) | (2 << 12) | (rd << 7) | 0x03


def ld(rd, rs1, imm):
    return ((imm & 0xFFF) << 20) | (rs1 << 15) | (3 << 12) | (rd << 7) | 0x03


def sw(rs2, rs1, imm):
    return (((imm >> 5) & 0x7F) << 25) | (rs2 << 20) | (rs1 << 15) | (2 << 12) | ((imm & 0x1F) << 7) | 0x23


def sd(rs2, rs1, imm):
    return (((imm >> 5) & 0x7F) << 25) | (rs2 << 20) | (rs1 << 15) | (3 << 12) | ((imm & 0x1F) << 7) | 0x23


def slli(rd, rs1, sh):
    return (sh << 20) | (rs1 << 15) | (1 << 12) | (rd << 7) | 0x13


def add(rd, rs1, rs2):
    return (rs2 << 20) | (rs1 << 15) | (rd << 7) | 0x33


def bge(rs1, rs2, off):
    b = off & 0x1FFF
    return (((b >> 12) & 1) << 31) | (((b >> 5) & 0x3F) << 25) | (rs2 << 20) | (rs1 << 15) | (5 << 12) | (((b >> 1) & 0xF) << 8) | ((b & 1) << 7) | 0x63


def jalr(rs1):
    return (rs1 << 15) | 0x67


T0, T1, T2, T3 = 5, 6, 7, 28
# t0 = 0x600000000
code = w(
    addi(T0, 0, (BUF_VA >> 32) & 0x7FF),
    slli(T0, T0, 32),
    lw(T1, T0, 8),          # count
    addi(T2, 0, 400),
    bge(T1, T2, 0x18),      # skip 6 instrs (24B): slli,add,sd,sd,sd,addi -> land on sw? no: land after sw
    slli(T2, T1, 5),
    add(T2, T2, T0),
    sd(10, T2, 16),
    sd(11, T2, 24),
    sd(12, T2, 32),
    addi(T1, T1, 1),
    sw(T1, T0, 8),
    ld(T3, T0, 0),          # orig
    jalr(T3),
)
# fix bge target: skip 7 instrs after bge = slli,add,sd,sd,sd,addi,sw (7*4=28=0x1C)
words = [addi(T0, 0, (BUF_VA >> 32) & 0x7FF), slli(T0, T0, 32), lw(T1, T0, 8), addi(T2, 0, 400),
         bge(T1, T2, 0x1C), slli(T2, T1, 5), add(T2, T2, T0), sd(10, T2, 16), sd(11, T2, 24), sd(12, T2, 32),
         addi(T1, T1, 1), sw(T1, T0, 8), ld(T3, T0, 0), jalr(T3)]
code = w(*words)
print('stub %d bytes' % len(code))

# --- write stub file on board fs ---
STUB_PATH = '/mnt/data/kmini/flush_stub.bin'
with open(STUB_PATH, 'wb') as f:
    f.write(code)

# --- fixed RW buffer at BUF_VA ---
libc = ctypes.CDLL('libc.so.6', use_errno=True)
libc.mmap.restype = ctypes.c_void_p
libc.mmap.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_long]
MAP_FIXED = 0x10
bufp = libc.mmap(BUF_VA, 0x10000, 3, 0x22 | MAP_FIXED, -1, 0)
assert bufp == BUF_VA, 'fixed mmap failed: %r' % bufp
# [0..8]=orig ptr, [8..12]=count
ctypes.memmove(BUF_VA, struct.pack('<Q', orig), 8)
ctypes.memmove(BUF_VA + 8, struct.pack('<I', 0), 4)

# --- mmap stub file PROT_READ|PROT_EXEC ---
fd = os.open(STUB_PATH, os.O_RDONLY)
stub_va = libc.mmap(None, 4096, 1 | 4, 0x02, fd, 0)
print('stub_va=0x%x' % stub_va)

saved = struct.unpack('<Q', ctypes.string_at(got, 8))[0]
print('GOT before 0x%x' % saved)
ctypes.memmove(got, struct.pack('<Q', stub_va), 8)

# --- load & run slice model ---
itp = nn.Interpreter()
itp.load_model(open('/mnt/data/kmini/s3c.kmodel', 'rb').read())
rng = np.random.default_rng(9)
feeds = [
    (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32),
    np.zeros((1, 1, 1, 33), np.float32),
    np.array([[21]], np.int32),
    (rng.standard_normal((48, 64, 32)) * 0.2).astype(np.float32),
    (rng.standard_normal((48, 32, 64)) * 0.2).astype(np.float32),
]
for i, t in enumerate(feeds):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
itp.run()
cnt_after_load = struct.unpack('<I', ctypes.string_at(BUF_VA + 8, 4))[0]
print('flush calls during load: %d' % cnt_after_load)
t0 = time.time()
itp.run()
dt = (time.time() - t0) * 1000

cnt = struct.unpack('<I', ctypes.string_at(BUF_VA + 8, 4))[0]
print('run took %.1fms; total flush calls: %d (run phase: %d)' % (dt, cnt, cnt - cnt_after_load))
n = min(cnt, 400)
raw = ctypes.string_at(BUF_VA + 16, n * 32)
vals = struct.unpack('<%dQ' % (n * 4), raw)
for i in range(n):
    phy, virt, size = vals[i * 4], vals[i * 4 + 1], vals[i * 4 + 2]
    print('  #%d phy=0x%x virt=0x%x size=%d (%.2fMB)' % (i, phy, virt, size, size / 1048576.0))
ctypes.memmove(got, struct.pack('<Q', saved), 8)
print('GOT restored')
