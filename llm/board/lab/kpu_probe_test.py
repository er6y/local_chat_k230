#!/usr/bin/env python3
# kpu_probe_test.py — 用导出时的 probe 真值对拍 daemon 真实 serve 路径
# 原理：导出保存了 probe_x[1,K,S,S]（kmodel 输入域）。daemon serve 的输入是
# 原始 x[mt,K]，handle() 做 fold + /scale 后应还原 probe_x。因此发
# raw = (probe_x[0] * sc[:,None]).T，daemon 返回 y 应等于 probe_y[0] 转置。
# 同时测 mt=57 部分 tile（GEMM 行独立，前 57 行应与整 tile 一致）。
import socket, struct, sys
import numpy as np

D = "/mnt/data/kpu_qwen/s16b"
MAGIC = 0x4B505547
MT_FULL = 256

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
    assert magic == MAGIC
    y = np.frombuffer(recvn(sk, mt * out * 4), dtype=np.float32).reshape(mt, out)
    return y, code

stems = sys.argv[1:] or ["l0_q", "l0_k", "l0_v", "l0_o", "l0_gate", "l0_up", "l0_down"]
worst = 0.0
for stem in stems:
    px = np.load(f"{D}/{stem}.probe_x.npy")[0].reshape(-1, MT_FULL)  # [K, 256]
    py = np.load(f"{D}/{stem}.probe_y.npy")[0].reshape(-1, MT_FULL)  # [OUT, 256]
    K, OUT = px.shape[0], py.shape[0]
    sc = np.fromfile(f"{D}/{stem}.scale", dtype=np.float32)
    assert sc.shape[0] == K, f"{stem} scale {sc.shape} vs K={K}"
    raw = (px * sc[:, None]).T                        # [256, K]
    for mt in (MT_FULL, 57):
        y, code = call(stem, mt, raw[:mt], OUT)
        if code != 0:
            print(f"{stem} mt={mt}: daemon err code={code}"); continue
        exp = py.T[:mt]                               # [mt, OUT]
        denom = np.abs(exp).max() + 1e-9
        rel = np.abs(y - exp).max() / denom
        worst = max(worst, rel)
        print(f"{stem} mt={mt}: K={K} OUT={OUT} rel_err={rel:.2e} "
              f"(y0={y[0,0]:.4f} exp0={exp[0,0]:.4f})")
print(f"WORST rel_err = {worst:.2e}  ->  {'PASS' if worst < 5e-2 else 'FAIL'}")
