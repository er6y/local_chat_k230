#!/usr/bin/env python3
# kpu_liveness.py — 不依赖参考数据的活性测试：
#   y_a = G(x);  y_b = G(-x);  y_c = G(x)
#   FROZEN : y_a==y_b==y_c（内核没跑，输出是陈旧缓冲）
#   LIVE   : y_b == -y_a 且 y_c == y_a（内核在算）
#   WEIRD  : y_c==y_a 但 y_b != ±y_a（在算但非线性/读错位）
import socket, struct, math
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

def call(stem, x, mt, K, OUT, timeout=30.0):
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
    stem = "l0_down"
    K, OUT, MT = 3072, 1024, 256
    n = MT * K
    x = array("f", [((i * 37 % 997) / 498.5 - 1.0) for i in range(n)])
    nx = array("f", [-v for v in x])
    y_a, c = call(stem, x, MT, K, OUT)
    print("call1 code=%s" % c)
    if y_a is None: return
    y_b, c = call(stem, nx, MT, K, OUT)
    print("call2(-x) code=%s" % c)
    if y_b is None: return
    y_c, c = call(stem, x, MT, K, OUT)
    print("call3(x) code=%s" % c)
    if y_c is None: return
    m = min(1 << 18, len(y_a))
    def stats(p, q):
        return max(abs(p[i] - q[i]) for i in range(0, m, 7)), \
               sum(p[i] * q[i] for i in range(0, m, 7))
    d_ab, s_ab = stats(y_a, y_b)    # negate: live => y_b = -y_a => |ya+yb|~0
    d_ac, s_ac = stats(y_a, y_c)    # repeat: deterministic => ~0
    print("max|a-b|=%.4g  max|a+b|=%.4g   (negate test: LIVE wants a+b~0)" % (d_ab, -s_ab))
    print("max|a-c|=%.4g                      (repeat test: DETERM wants a-c~0)" % d_ac)
    na = math.sqrt(sum(v * v for v in y_a[:65536]))
    nb = math.sqrt(sum(v * v for v in y_b[:65536]))
    print("|y_a|=%.4g |y_b|=%.4g  y_a[0:4]=%s" % (na, nb, ["%.4g" % v for v in y_a[:4]]))
    if d_ac < 1e-3 and d_ab < 1e-3:
        print("VERDICT: FROZEN —— 内核没执行，输出=陈旧缓冲")
    elif -s_ab > 0.9 * na * nb and d_ac < 1e-2 * max(na, 1e-9):
        print("VERDICT: LIVE —— 内核在算且确定（数值错=量化/导出层）")
    elif d_ac < 1e-2 * max(na, 1e-9):
        print("VERDICT: RESPONSIVE-NOT-LINEAR —— 输出随输入变但非 x@W.T（读位/布局错）")
    else:
        print("VERDICT: NONDETERMINISTIC —— 输出不稳定（缓冲混叠/竞态）")

main()
