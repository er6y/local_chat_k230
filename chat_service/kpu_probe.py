#!/usr/bin/env python3
# kpu_probe3.py — 板上执行：直接对 kpud 做数值差分（不经过 llmd）
#   probe_x.npy: [1,K,16,16] c-major  -> 展开成 x[m][c]（行主序）送 mt 行
#   守护返回: y[m][OUT] 行主序（按 daemon copy-out 的写法）
#   参考:     probe_y.npy [1,OUT,16,16] c-major -> py[o][256]
#   比较:     y[m*OUT+o] vs py[o*MT+m]
# 仅 stdlib（板上无 numpy）。用途：判断"部分填充 tile(mt<256)"数值是否正确。
import socket, struct, sys, math
from array import array

SOCK = "/tmp/kpu_gemm.sock"
MAGIC = 0x4B505547

def load_npy_f32(path):
    with open(path, "rb") as f:
        if f.read(6) != b"\x93NUMPY":
            raise SystemExit("not npy: " + path)
        major, minor = struct.unpack("BB", f.read(2))
        hlen = struct.unpack("<H", f.read(2))[0] if major == 1 else struct.unpack("<I", f.read(4))[0]
        hdr = f.read(hlen).decode("latin1")
        d = eval(hdr.strip(), {"False": False, "True": True})
        n = 1
        for s in d["shape"]:
            n *= int(s)
        a = array("f")
        a.frombytes(f.read(4 * n))
        return a

def recv_exact(s, n):
    buf = bytearray()
    while len(buf) < n:
        b = s.recv(n - len(buf))
        if not b:
            raise SystemExit("closed early")
        buf += b
    return bytes(buf)

def call(stem, x, mt, K, OUT, timeout=30.0):
    s = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    s.settimeout(timeout)
    s.connect(SOCK)
    s.sendall(struct.pack("<IH", MAGIC, len(stem)) + stem.encode() +
              struct.pack("<I", mt) + x[:mt * K].tobytes())
    magic, code = struct.unpack("<II", recv_exact(s, 8))
    if magic != MAGIC:
        s.close(); return None, -1
    if code != 0:
        s.close(); return None, code
    y = array("f")
    y.frombytes(recv_exact(s, mt * OUT * 4))
    s.close()
    return y, 0

def main():
    stem = sys.argv[1] if len(sys.argv) > 1 else "l0_down"
    base = sys.argv[2] if len(sys.argv) > 2 else "/mnt/data/kpu_qwen"
    K = int(sys.argv[3]) if len(sys.argv) > 3 else 3072
    OUT = int(sys.argv[4]) if len(sys.argv) > 4 else 1024
    MT = 256
    px = load_npy_f32("%s/%s.probe_x.npy" % (base, stem))
    py = load_npy_f32("%s/%s.probe_y.npy" % (base, stem))
    print("%s K=%d OUT=%d: probe n=%d/%d" % (stem, K, OUT, len(px), len(py)))
    if len(px) != K * MT or len(py) != OUT * MT:
        raise SystemExit("probe size mismatch (want x=%d y=%d)" % (K * MT, OUT * MT))
    x = array("f", bytes(4 * MT * K))
    for m in range(MT):
        for c in range(K):
            x[m * K + c] = px[c * MT + m]
    print("unfolded; probing mt=256/57/18/1")
    for mt in (256, 57, 18, 1):
        y, code = call(stem, x, mt, K, OUT)
        if y is None:
            print("  mt=%-4d REFUSED code=%d" % (mt, code)); continue
        dot = ny = nr = maxe = 0.0
        for m in range(mt):
            for o in range(OUT):
                a = y[m * OUT + o]; b = py[o * MT + m]
                dot += a * b; ny += a * a; nr += b * b
                e = abs(a - b)
                if e > maxe: maxe = e
        cos = dot / (math.sqrt(ny) * math.sqrt(nr) + 1e-12)
        print("  mt=%-4d cos=%.6f max|dy|=%.4f |y|=%.2f |ref|=%.2f | y[0..2]=%.3f %.3f %.3f ref=%.3f %.3f %.3f"
              % (mt, cos, maxe, math.sqrt(ny), math.sqrt(nr),
                 y[0], y[1], y[2], py[0], py[MT], py[2 * MT]))

main()
