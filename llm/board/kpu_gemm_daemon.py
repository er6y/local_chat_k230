#!/usr/bin/python3
"""kpu_gemm_daemon.py - serve Qwen GEMM kmodels over a unix socket using the
OFFICIAL vendor runtime (nncaseruntime) that is proven correct on this board
(cos 0.99 on hardware). llama.cpp's kpu_invoke_tile talks to us instead of the
broken github static runtime.

request:  MAGIC u32 | stemlen u16 | stem | mt u32 | x[mt*K] f32
response: MAGIC u32 | code u32  | y[mt*OUT] f32   (code 0 = ok)

stem = e.g. "l0_q". mt<=16 -> S4 kmodel (s4_sq), else S16 (s16_sq).
Fold + smoothquant compensation happen here; caller sends raw x[m][k]."""
import os
import signal
import socket
import time
import struct
import sys
import time

import numpy as np
import nncaseruntime as nn

SOCK = "/tmp/kpu_gemm.sock"
D_S1 = "/mnt/data/kpu_qwen/s1_sq"    # M=1 exact tile: pure weight-DMA floor
D_S4 = "/mnt/data/kpu_qwen/s4_sq"
D_S16 = "/mnt/data/kpu_qwen/s16_sq"
# measured per-interpreter CMA: S1 ~3.6MB, S4 ~6.4MB. Budget ~850MB of the
# 1GB pool: S1 full set (196x3.6=706MB, decode never thrashes) + S4 20-slot
# LRU (128MB; prefill passes thrash through it, tolerable once per prompt).
CAPS = {1: int(os.environ.get("KPUD_CAP1", "196")),
        4: int(os.environ.get("KPUD_CAP4", "20")),
        16: int(os.environ.get("KPUD_CAP16", "0"))}
VERBOSE = os.environ.get("KPUD_QUIET") is None

cache = {}          # (stem, S) -> interpreter
order = []
stats = {"calls": 0, "tiles": 0, "us": 0, "errs": 0}
_ph = {}
shapes = {}         # stem -> (K, OUT) learned from first call


def log(*a):
    if VERBOSE:
        print("[kpwd]", *a, flush=True)


def get_interp(stem, S):
    key = (stem, S)
    it = cache.get(key)
    if it:
        return it
    d = {1: D_S1, 4: D_S4, 16: D_S16}[S]
    path = f"{d}/{stem}.kmodel"
    if not os.path.exists(path):
        return None
    # two-tier LRU: the S=1 decode set stays fully resident (per-interpreter
    # ~3MB -> 196 x 3 = 590MB fits the 1GB CMA; zero reload thrash per token).
    # S=4 prefill set caps low (~6.4MB/interp would need 1.25GB for all 196);
    # prefill LRU-thrashes instead - one pass per prompt tolerates it.
    if len([k for k in order if k[1] == S]) >= CAPS[S]:
        victim = next(k for k in order if k[1] == S)
        order.remove(victim)
        cache.pop(victim, None)
    interp = nn.Interpreter()
    interp.load_model(open(path, "rb").read())
    sh = [int(v) for v in interp.get_input_shape(0)]
    outsh = [int(v) for v in interp.get_output_shape(0)]
    cache[key] = interp
    order.append(key)
    shapes[stem] = (sh[1], outsh[1])
    log(f"loaded {path} in={[int(v) for v in sh]} out={outsh}")
    return interp


def load_scale(stem, S):
    d = {1: D_S1, 4: D_S4, 16: D_S16}[S]
    p = f"{d}/{stem}.scale"
    if os.path.exists(p):
        return np.fromfile(p, dtype=np.float32)
    return None


SCALES = {}


def handle(stem, mt, x):
    # decode (mt==1) rides the S=1 set: exact-fit tile, tensors 16x smaller,
    # per-GEMM at the weight-DMA floor (~0.7-1.9ms measured)
    S = 1 if mt == 1 else (4 if mt <= 16 else 16)
    interp = get_interp(stem, S)
    if interp is None:
        return None, f"no kmodel {stem} S{S}"
    _t_f0 = time.time_ns()
    K, OUT = shapes[stem]
    if x.size != mt * K:
        return None, f"size {x.size} != {mt}*{K}"
    MT = S * S
    key = (stem, S)
    if key not in SCALES:
        SCALES[key] = load_scale(stem, S)
    sc = SCALES[key]
    # fold x[m][k] -> in[1,K,S,S]; zero-pad m in [mt, MT)
    xm = x.reshape(mt, K)
    inp = np.zeros((1, K, MT), dtype=np.float32)
    inp[0, :, :mt] = xm.T          # [K, mt]
    if sc is not None:
        # classic SmoothQuant direction: shrink outlier channels (x/s);
        # the kmodel has W*s baked. The old x*s direction AMPLIFIED outliers
        # and was the root cause of the garbled output (down GEMMs died).
        inp[0, :, :mt] /= sc.reshape(K, 1)
    inp = inp.reshape(1, K, S, S)
    _t_f1 = time.time_ns()
    t = nn.RuntimeTensor.from_numpy(inp)
    interp.set_input_tensor(0, t)
    _t_r0 = time.time_ns()
    interp.run()
    _t_r1 = time.time_ns()
    out = interp.get_output_tensor(0).to_numpy()   # [1,OUT,S,S]
    _t_o1 = time.time_ns()
    _ph["fold"] = _ph.get("fold", 0) + (_t_f1 - _t_f0) // 1000
    _ph["run"] = _ph.get("run", 0) + (_t_r1 - _t_r0) // 1000
    _ph["out"] = _ph.get("out", 0) + (_t_o1 - _t_r1) // 1000
    y = out.reshape(OUT, MT)[:, :mt].T.copy()      # [mt, OUT]
    return y.reshape(-1), None


def serve():
    if os.path.exists(SOCK):
        os.unlink(SOCK)
    sk = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    sk.bind(SOCK)
    sk.listen(4)
    # RT priority: llama-cli blocks on recv() the moment it sends a request;
    # FIFO makes the kernel preempt llama and schedule us instantly instead of
    # waiting a scheduler tick on this single-core board (HZ=250 => ~2-4ms
    # average wake latency per round trip otherwise).
    # SCHED_FIFO measured WORSE end-to-end (7.3 vs 4.0 ms/call): the RT class
    # makes the daemon's own run() faster (1.76 vs 2.63 ms) but the socket
    # receive side balloons ~5 ms. Opt-in only.
    if os.environ.get("KPUD_FIFO") == "1":
        try:
            os.sched_setscheduler(0, os.SCHED_FIFO, os.sched_param(55))
            log("SCHED_FIFO(55) set (opt-in)")
        except Exception as e:
            log("sched fifo skipped:", e)
    log(f"listening on {SOCK}; tier caps {CAPS}")
    if os.environ.get("KPUD_WARM", "1") == "1":
        # pre-load the full S1 decode set once (~6-10s): first token is not
        # 196 serial loads, and any CMA overflow surfaces HERE, deterministically
        t0 = time.time_ns()
        n = 0
        for li in range(28):
            for tag in ("q", "k", "v", "o", "gate", "up", "down"):
                if get_interp(f"l{li}_{tag}", 1):
                    n += 1
        log(f"warmed {n} S1 interpreters in {(time.time_ns()-t0)/1e9:.1f}s "
            f"CmaFree={open('/proc/meminfo').read().split('CmaFree:')[1].split()[0]}kB")
        import gc
        gc.collect()
        gc.freeze()          # warmed interpreters become permanent: no GC scans
        gc.disable()         # kill periodic collector pauses in the serve loop
        def _clean_exit(sig, frm):
            log("SIGTERM: releasing cache (frees mmz/CMA)")
            cache.clear(); order.clear()
            gc.collect()
            os._exit(0)
        signal.signal(signal.SIGTERM, _clean_exit)
    while True:
        conn, _ = sk.accept()
        try:
            conn.settimeout(120)
            with conn:
                while True:
                    hdr = recvn(conn, 4 + 2)
                    if not hdr:
                        break
                    magic, slen = struct.unpack("<IH", hdr)
                    if magic != 0x4B505547:
                        break
                    stem = recvn(conn, slen).decode()
                    (mt,) = struct.unpack("<I", recvn(conn, 4))
                    K, OUT = shapes.get(stem, (0, 0))
                    if K == 0:  # learn shapes by peeking at the kmodel list
                        K = infer_K(stem)
                        OUT = infer_OUT(stem)
                        shapes[stem] = (K, OUT)
                    x = np.frombuffer(receiven(conn, mt * K * 4), dtype=np.float32)
                    t0 = time.time_ns()
                    try:
                        y, err = handle(stem, mt, x)
                    except Exception as e:
                        y, err = None, repr(e)
                    dt = (time.time_ns() - t0) // 1000
                    _ph["h"] = _ph.get("h", 0) + dt
                    stats["calls"] += 1
                    stats["tiles"] += mt
                    stats["us"] += dt
                    if err:
                        stats["errs"] += 1
                        conn.sendall(struct.pack("<II", 0x4B505547, 1) +
                                     err.encode()[:255])
                    else:
                        conn.sendall(struct.pack("<II", 0x4B505547, 0) +
                                     y.astype(np.float32).tobytes())
                    if stats["calls"] % 50 == 0:
                        n_ = max(1, stats["calls"])
                        log(f"calls={stats['calls']} avg={stats['us']//n_}us "
                            f"run={_ph.get('run',0)//n_} fold={_ph.get('fold',0)//n_} "
                            f"out={_ph.get('out',0)//n_} errs={stats['errs']}")
        except Exception as e:
            import traceback
            log("conn error:", e)
            traceback.print_exc(file=sys.stderr)


def recvn(conn, n):
    b = b""
    while len(b) < n:
        c = conn.recv(n - len(b))
        if not c:
            return None
        b += c
    return b


def receiven(conn, n):
    b = recvn(conn, n)
    if b is None:
        raise ConnectionError("eof")
    return b


SPEC = {"q": (1024, 2048), "k": (1024, 1024), "v": (1024, 1024), "o": (2048, 1024),
        "gate": (1024, 3072), "up": (1024, 3072), "down": (3072, 1024)}  # (K, OUT)


def infer_K(stem):
    return SPEC[stem.split("_", 1)[1]][0]


def infer_OUT(stem):
    return SPEC[stem.split("_", 1)[1]][1]


if __name__ == "__main__":
    while True:
        try:
            serve()
        except Exception as e:
            import traceback
            log("FATAL:", e)
            traceback.print_exc(file=sys.stderr)
            time.sleep(2)
            try:
                if os.path.exists(SOCK):
                    os.unlink(SOCK)
            except OSError:
                pass
