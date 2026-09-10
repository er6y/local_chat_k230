#!/usr/bin/env python3
# kpu_repro_test.py — GNNE int8 真机数值 bug 最小复现判定
# 前置：daemon 以 KPUD_S16_DIR=/mnt/data/kpu_qwen/repro 启动（只载迷你模型）
# 协议同生产：raw x[256,IN] -> daemon（m_f 会 /scale）-> kmodel -> y[256,OUT]
# 判定：y vs 导出时的 float32 Conv 参考（= W @ x，域自消）
import socket, struct, sys
import numpy as np

D = "/mnt/data/kpu_qwen/repro"
MAGIC = 0x4B505547
STEMS = [("r0_q", 1024, 2048), ("r1_q", 1024, 2048), ("r2_q", 1024, 2048),
         ("r3_q", 1024, 2048), ("r4_o", 2048, 1024), ("r5_down", 3072, 1024),
         ("r6_q", 1024, 2048), ("r7_q", 1024, 2048)]

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

print(f"{'stem':6} {'IN':>5} {'OUT':>5} {'rel_max':>9}  verdict")
worst = 0.0
for stem, IN, OUT in STEMS:
    xin = np.load(f"{D}/{stem}.xin.npy")          # [256, IN] raw 域
    ref = np.load(f"{D}/{stem}.ref.npy")          # [256, OUT] float 参考
    sk.sendall(struct.pack("<IH", MAGIC, len(stem)) + stem.encode()
               + struct.pack("<I", 256) + xin.astype(np.float32).tobytes())
    magic, code = struct.unpack("<II", recvn(sk, 8))
    if magic != MAGIC or code != 0:
        print(f"{stem:6} daemon err magic={hex(magic)} code={code}")
        continue
    y = np.frombuffer(recvn(sk, 256 * OUT * 4), dtype=np.float32).reshape(256, OUT)
    rel = np.abs(y - ref).max() / (np.abs(ref).max() + 1e-9)
    ok = rel < 0.05
    worst = max(worst, 0.0 if ok else rel)
    print(f"{stem:6} {IN:5} {OUT:5} {rel:9.3f}  {'OK' if ok else 'BROKEN'}")
print("SUMMARY:", "ALL PASS" if worst == 0.0 else f"{worst:.3f} rel err on device (sim exact)")
