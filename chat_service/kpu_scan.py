#!/usr/bin/env python3
# kpu_scan.py — 路由扫描：对单个输入元素 +0.5（锯齿周期 2.0 → 必现可见位移），
# 找输出里哪些 (row,o) 动了 → 揭示 输入→输出 的真实路由。
import socket, struct
from array import array

SOCK = "/tmp/kpu_gemm.sock"
MAGIC = 0x4B505547

def recv_exact(s, n):
    buf = bytearray()
    while len(buf) < n:
        b = s.recv(n - len(buf))
        if not b:
            raise SystemExit("closed early")
        buf += b
    return bytes(buf)

def call(stem, x, mt, K, OUT, timeout=60.0):
    s = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    s.settimeout(timeout)
    s.connect(SOCK)
    s.sendall(struct.pack("<IH", MAGIC, len(stem)) + stem.encode() +
              struct.pack("<I", mt) + x[:mt * K].tobytes())
    magic, code = struct.unpack("<II", recv_exact(s, 8))
    if magic != MAGIC or code != 0:
        s.close()
        return None, code
    y = array("f")
    y.frombytes(recv_exact(s, mt * OUT * 4))
    s.close()
    return y, 0

def main():
    stem, K, OUT, MT = "l0_down", 3072, 1024, 256
    n = MT * K
    x = array("f", [((i * 37 % 997) / 498.5 - 1.0) for i in range(n)])
    y0, code = call(stem, x, MT, K, OUT)
    if y0 is None:
        print("REFUSED", code); return

    perts = [(0, 0, 0.5), (0, 1, 0.5), (0, 2, 0.5), (1, 0, 0.5),
             (5, 0, 0.5), (0, 100, 0.5), (0, 2048, 0.5), (3, 700, 0.5)]
    for (m, c, d) in perts:
        xp = array("f", x); xp[m * K + c] += d
        yp, _ = call(stem, xp, MT, K, OUT)
        if yp is None:
            print("pert(%d,%d) REFUSED" % (m, c)); continue
        hits = []
        for row in range(MT):
            base = row * OUT
            for o in range(OUT):
                dy = yp[base + o] - y0[base + o]
                if abs(dy) > 0.02:
                    hits.append((abs(dy), row, o, dy))
        hits.sort(reverse=True)
        if not hits:
            print("pert x[%d][%d]+%.1f → 无任何输出变化" % (m, c, d))
        else:
            top = ", ".join("y[%d][%d]%+.3f" % (r, o, dv) for (_, r, o, dv) in hits[:4])
            print("pert x[%d][%d]+%.1f → %d 处变化; %s" % (m, c, d, len(hits), top))

main()
