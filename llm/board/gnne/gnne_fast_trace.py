#!/usr/bin/env python3
"""gnne_fast_trace.py - mmap the GNNE window and poll status at ~100us while a
selftest runs in another process. devmem-based sampling (~10ms) can miss a
2ms KPU execution window; this catches any transient running state."""
import ctypes, os, subprocess, time, threading

libc = ctypes.CDLL("libc.so.6", use_errno=True)
libc.mmap.restype = ctypes.c_void_p
libc.mmap.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_int,
                      ctypes.c_int, ctypes.c_int, ctypes.c_long]
fd = os.open("/dev/mem", os.O_RDWR | os.O_SYNC)
base = libc.mmap(None, 0x1000, 0x3, 0x1, fd, 0x80400000)
st130 = ctypes.c_uint32.from_address(base + 0x130)
st50 = ctypes.c_uint32.from_address(base + 0x50)
pc100 = ctypes.c_uint32.from_address(base + 0x100)
ctrl128 = ctypes.c_uint64.from_address(base + 0x128)
print(f"window 0x{base:x}", flush=True)

hits = []
stop = False

def sampler():
    last = None
    n = 0
    while not stop:
        s = st130.value
        if s != last:
            hits.append((n, time.time(), hex(s), hex(pc100.value)))
            last = s
        n += 1

t = threading.Thread(target=sampler)
t.start()

env = dict(os.environ)
env.update(LD_LIBRARY_PATH="/mnt/data/kpu_llm",
           KPU_KMODEL_DIR="/mnt/data/kpu_qwen_sqtest",
           KPU_RESIDENT="10", KPU_TILES="s4", KPU_PRELOAD="0",
           KPU_SELFTEST="1", LD_BIND_NOW="1")
p = subprocess.run(
    ["/mnt/data/kpu_llm/llama-cli", "-m", "/mnt/data/models/qwen3-q4km.gguf",
     "-c", "256", "-p", "你好", "-n", "1", "-st", "--simple-io"],
    env=env, capture_output=True, timeout=180)
stop = True
t.join()
out = p.stderr.decode(errors="replace")
print(f"rc={p.returncode}")
for line in out.splitlines():
    if "cos=" in line or "preseed" in line:
        print(line)
print(f"--- st130 transitions: {len(hits)} ---")
for h in hits[:20]:
    print(h)
