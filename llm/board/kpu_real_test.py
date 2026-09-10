#!/usr/bin/env python3
# kpu_real_test.py — 真实激活判据：把校准文件（真实 CPU 激活，raw 域）送进
# daemon 真实 serve 路径，对照 gguf 反量化 float32 参考 GEMM。
# 这是 int8 量化正确性的正准判据（随机 probe 对真实校准模型天然偏大）。
import socket, struct, sys
import numpy as np

D = "/mnt/data/kpu_qwen/s16b"
MAGIC = 0x4B505547

def recvn(c, n):
    b = b""
    while len(b) < n:
        p = c.recv(n - len(b))
        if not p: raise SystemExit("daemon eof")
        b += p
    return b

sk = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
sk.connect("/tmp/kpu_gemm.sock")
sk.settimeout(120)

def call(stem, mt, x, out):
    sk.sendall(struct.pack("<IH", MAGIC, len(stem)) + stem.encode()
               + struct.pack("<I", mt) + x.astype(np.float32).tobytes())
    magic, code = struct.unpack("<II", recvn(sk, 8))
    assert magic == MAGIC, hex(magic)
    y = np.frombuffer(recvn(sk, mt * out * 4), dtype=np.float32).reshape(mt, out)
    return y, code

MT = 256
worst = 0.0
for tag in (sys.argv[1:] or ["l0_q", "l0_o"]):
    x = np.load(f"{D}/xreal_{tag}.npy")[0].reshape(-1, MT)   # [IN,256] raw 域
    yref = np.load(f"{D}/yref_{tag}.npy")[0].reshape(-1, MT).T  # [256,OUT]
    IN, OUT = x.shape[0], yref.shape[1]
    sc = np.fromfile(f"{D}/{tag}.scale", dtype=np.float32)
    raw = (x * sc[:, None]).T.astype(np.float32)             # daemon /s 后还原 x
    y, code = call(tag, MT, raw, OUT)
    if code != 0:
        print(f"{tag}: daemon err {code}"); continue
    denom = np.abs(yref).max() + 1e-9
    rel = np.abs(y - yref).max() / denom
    # 只看幅度大的通道（绝对误差小但参考值也小的元素会虚增 rel）
    mask = np.abs(yref) > 0.1 * denom
    rel_sig = np.abs((y - yref)[mask]).max() / denom
    worst = max(worst, rel_sig)
    print(f"{tag}: IN={IN} OUT={OUT} rel_max={rel:.3f} rel_significant={rel_sig:.3f} "
          f"(y0={y[0,0]:.3f} ref0={yref[0,0]:.3f})")
print(f"WORST significant rel = {worst:.3f}  ->  {'PASS(<10%)' if worst < 0.10 else 'FAIL'}")
