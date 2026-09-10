#!/usr/bin/env python3
# kpu_leak_test.py — daemon 每 RPC 调用 CMA/RSS 泄漏定位
# 用法: python3 kpu_leak_test.py [ncalls] [mt]
# 每次 10 调用采样 /proc/meminfo CmaFree 与自身 RSS（对照组，daemon 进程
# 的 RSS 看板上是另一进程——这里同时抓 daemon pid 的 VmRSS）。
import socket, struct, sys, os, time
import numpy as np

SOCK = "/tmp/kpu_gemm.sock"
MAGIC = 0x4B505547
# 用法: kpu_leak_test.py [ncalls] [mt] [stem:K:OUT,stem:K:OUT,...]
BATCHES = []
if len(sys.argv) > 3:
    for spec in sys.argv[3].split(","):
        s, k, o = spec.split(":")
        BATCHES.append((s, int(k), int(o)))
else:
    BATCHES = [("l0_q", 1024, 2048)]
N = int(sys.argv[1]) if len(sys.argv) > 1 else 40
MT = int(sys.argv[2]) if len(sys.argv) > 2 else 32

def daemon_pid():
    for l in os.popen("ps -w").read().splitlines():
        if "kpu_gemm_daemon.py" in l and "grep" not in l:
            return l.split()[0]
    return None

def sample(tag):
    cf = rf = rss = "?"
    for l in open("/proc/meminfo"):
        if l.startswith("CmaFree"): cf = l.split()[1]
    pid = daemon_pid()
    if pid:
        try:
            for l in open(f"/proc/{pid}/status"):
                if l.startswith("VmRSS"): rss = l.split()[1]
        except Exception: pass
    print(f"{tag}: CmaFree={cf}kB daemonRSS={rss}kB", flush=True)

sk = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
sk.connect(SOCK)
payloads = []
for stem, k, out in BATCHES:
    x = (np.arange(MT * k, dtype=np.float32) % 7) / 7.0 - 0.35
    payloads.append((stem, k, out,
        struct.pack("<IH", MAGIC, len(stem)) + stem.encode() + struct.pack("<I", MT) + x.tobytes()))
sk.settimeout(30)
t0 = time.time()
for i in range(1, N + 1):
    stem, k, out, payload = payloads[(i - 1) % len(payloads)]
    sk.sendall(payload)
    hdr = b""
    while len(hdr) < 8:
        c = sk.recv(8 - len(hdr))
        if not c: raise SystemExit("daemon closed")
        hdr += c
    magic, code = struct.unpack("<II", hdr)
    assert magic == MAGIC, hex(magic)
    y = b""
    need = MT * out * 4
    while len(y) < need:
        c = sk.recv(need - len(y))
        if not c: raise SystemExit("daemon closed mid-y")
        y += c
    if code != 0: print(f"call {i} {stem}: err code={code}")
    if i % 10 == 0: sample(f"call {i}")
print(f"done {N} calls in {time.time()-t0:.1f}s", flush=True)
