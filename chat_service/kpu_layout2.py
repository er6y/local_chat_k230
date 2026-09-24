#!/usr/bin/env python3
# kpu_layout2.py — 输出性质判定 v2（修越界）：
#  P1 扰动 x[0][5]   → 看行0输出：跳1个元素=回声(in[o][m]别名)；全行微变=GEMM
#  P2 扰动 x[1][5]   → 行1输出（跨行对照）
#  P3 扰动 x[5][0]   → 回声预测 y[5][0] 单点跳变；GEMM 预测行5全变
#  D  diff 剖面 o=0..40 与整行统计（前次发现 y=x+1.2156 只在开头成立）
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

def profile(y, y2, row, OUT, label):
    d = [abs(y2[row * OUT + o] - y[row * OUT + o]) for o in range(OUT)]
    mx = max(d); amx = d.index(mx)
    print("%s: max=%.4f @o=%d   >0.05:%d  >0.005:%d  mean=%.5f" %
          (label, mx, amx, sum(1 for v in d if v > 0.05),
           sum(1 for v in d if v > 0.005), sum(d) / OUT))
    return d

def main():
    stem, K, OUT, MT = "l0_down", 3072, 1024, 256
    n = MT * K
    x = array("f", [((i * 37 % 997) / 498.5 - 1.0) for i in range(n)])
    y, code = call(stem, x, MT, K, OUT)
    if y is None:
        print("REFUSED", code); return

    # D: diff 剖面（y - x，行0，o=0..40）+ 整行统计
    d0 = [y[o] - x[o] for o in range(48)]
    print("D y0[o]-x0[o] o=0..15: %s" % ["%.3f" % v for v in d0[:16]])
    print("D y0[o]-x0[o] o=16..31: %s" % ["%.3f" % v for v in d0[16:32]])
    print("D y0[o]-x0[o] o=32..47: %s" % ["%.3f" % v for v in d0[32:48]])
    dall = [y[o] - x[o] for o in range(OUT)]
    print("D 整行: mean=%.4f std[手工]=%.4f  min=%.3f max=%.3f" % (
        sum(dall) / OUT,
        (sum((v - sum(dall) / OUT) ** 2 for v in dall) / OUT) ** 0.5,
        min(dall), max(dall)))

    # P1: 扰动 m0,c5
    x2 = array("f", x); x2[5] += 1.0
    y2, _ = call(stem, x2, MT, K, OUT)
    d = profile(y, y2, 0, OUT, "P1 扰动x[0][5] 行0")
    print("   变化最大的5个 o: %s" % sorted(range(OUT), key=lambda o: -d[o])[:5])

    # P2: 扰动 m1,c5
    x3 = array("f", x); x3[K + 5] += 1.0
    y3, _ = call(stem, x3, MT, K, OUT)
    profile(y, y3, 1, OUT, "P2 扰动x[1][5] 行1")
    d_r0 = max(abs(y3[o] - y[o]) for o in range(OUT))
    print("   行0 对照 max|dy|=%.4f" % d_r0)

    # P3: 扰动 m5,c0 → 回声预测跳 y[5][0]
    x4 = array("f", x); x4[5 * K] += 1.0
    y4, _ = call(stem, x4, MT, K, OUT)
    d5 = profile(y, y4, 5, OUT, "P3 扰动x[5][0] 行5")
    print("   y[5][0] dy=%.4f；变化最大3个 o: %s" %
          (y4[5 * OUT] - y[5 * OUT], sorted(range(OUT), key=lambda o: -d5[o])[:3]))

main()
