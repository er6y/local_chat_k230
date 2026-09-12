#!/usr/bin/env python3
# gguf_scan_min.py — 无依赖 GGUF 头解析：张量类型直方图 + 关键张量（板端取证用）
# 用途：确认 board 上 gguf 的真实量化构成（body / output / token_embd 各是什么）
import struct, collections, sys

p = sys.argv[1] if len(sys.argv) > 1 else "/mnt/data/models/Qwen3-0.6B-Q8_0_q6k_tied.gguf"
f = open(p, 'rb')
assert f.read(4) == b'GGUF'
ver = struct.unpack('<I', f.read(4))[0]
n_tensors = struct.unpack('<Q', f.read(8))[0]
n_kv = struct.unpack('<Q', f.read(8))[0]

def rd_str():
    l = struct.unpack('<Q', f.read(8))[0]
    return f.read(l).decode('utf-8', 'replace')

SZ = {0: 1, 1: 1, 2: 2, 3: 2, 4: 4, 5: 4, 6: 4, 7: 1, 10: 8, 11: 8, 12: 8}

def skip(tp):
    if tp == 8:
        rd_str()
    elif tp == 9:
        et = struct.unpack('<I', f.read(4))[0]
        n = struct.unpack('<Q', f.read(8))[0]
        for _ in range(n):
            skip(et)
    else:
        f.read(SZ[tp])

for _ in range(n_kv):
    rd_str()
    tp = struct.unpack('<I', f.read(4))[0]
    skip(tp)

TYPES = {0: 'F32', 1: 'F16', 2: 'Q4_0', 3: 'Q4_1', 6: 'Q5_0', 7: 'Q5_1', 8: 'Q8_0',
         9: 'Q8_1', 10: 'Q2_K', 11: 'Q3_K', 12: 'Q4_K', 13: 'Q5_K', 14: 'Q6_K',
         15: 'Q8_K', 16: 'IQ2_XXS', 30: 'BF16'}
BPW = {0: 32, 1: 16, 2: 4.5, 3: 5, 6: 5.5, 7: 6, 8: 8.5, 9: 9, 10: 2.5625, 11: 3.4375,
       12: 4.5, 13: 5.5, 14: 6.5625, 15: 9, 30: 16}

cnt = collections.Counter()
nbytes = collections.Counter()
ts = []
for _ in range(n_tensors):
    name = rd_str()
    nd = struct.unpack('<I', f.read(4))[0]
    dims = [struct.unpack('<Q', f.read(8))[0] for _ in range(nd)]
    tp = struct.unpack('<I', f.read(4))[0]
    struct.unpack('<Q', f.read(8))[0]   # offset
    n = 1
    for d in dims:
        n *= d
    tn = TYPES.get(tp, tp)
    cnt[tn] += 1
    nbytes[tn] += n * BPW.get(tp, 8) / 8
    ts.append((name, tn, tuple(dims)))

print("file:", p, "ver", ver, "tensors", n_tensors)
print("=== type histogram ===")
for k, v in cnt.most_common():
    print("  %-8s n=%-4d  ~%.1f MB" % (k, v, nbytes[k] / 1e6))
print("=== key tensors ===")
for name, tp, dims in ts:
    if 'token_embd' in name or name.startswith('output') or 'blk.0.' in name:
        print("  %-38s %-6s %s" % (name, tp, dims))
print("SCAN_OK")
