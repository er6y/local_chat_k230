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
import mmap
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
D_S16 = "/mnt/data/kpu_qwen/s16b"    # era-B 重导出（mk_sq16b.py，W*s + x/s）；老 s16_sq 是 era-A 方向，乱码
# 内存实测（2026-09，drop_caches 后）：S16 era-B 全量 196 个 = 450MB
# （2.29MB/interp），加载 21s；载完 CmaFree 仍余 438MB，与 TTS（~150MB）
# 共存无压力。此前的"装不下"是页缓存挤占 CMA 区的假象（drop_caches 回收
# ~320MB）。生产静态配比：CAP16=196, CAP4=0, CAP1=0（decode M<32 走 CPU）。
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
    if CAPS[S] <= 0:          # 容量 0 = 该层完全不 serve（如 CAP1=0 省 700MB
        return None           # CMA，llmd 劫持只吃 M>=32 的 S4 prefill 时用）
    # two-tier LRU: the S=1 decode set stays fully resident (per-interpreter
    # ~3MB -> 196 x 3 = 590MB fits the 1GB CMA; zero reload thrash per token).
    # S=4 prefill set caps low (~6.4MB/interp would need 1.25GB for all 196);
    # prefill LRU-thrashes instead - one pass per prompt tolerates it.
    # 驱逐=释放解释器，实测这条路径会段错误（nncase/mmz 释放即崩），一崩
    # 客户端拿到错误码、若客户端没回退就直接输出未初始化内存（乱码根因）。
    # 所以改成"满容量直接拒绝"，让客户端回退 CPU：慢但永远正确。
    if len([k for k in order if k[1] == S]) >= CAPS[S]:
        log(f"REFUSED {path}: S{S} at cap {CAPS[S]} (no evict: free crashes)")
        return None
    interp = nn.Interpreter()
    try:
        data = _read_kmodel_nocache(path)
        interp.load_model(data)
        del data
    except Exception as e:
        # CMA 耗尽/坏文件：跳过该 GEMM（客户端 CPU 回退），绝不让单个加载
        # 失败炸掉整个 daemon——崩溃死亡会泄漏全部已载 mmz 段，恶性循环
        log(f"LOAD FAIL {path}: {e}")
        return None
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


def _read_kmodel_nocache(path):
    """普通读 + 立即 fadvise 驱逐页缓存。读缓存在两次加载间被消化，
    zone 高阶块留得住（196 个 22.9s 全量通过实测）。
    O_DIRECT 变体（KPUD_ODIRECT=1）在本板 ext4+mmc 栈上会 EINVAL 甚至
    挂死（实录），默认禁用；仅块设备裸读验证过可行。"""
    if os.environ.get("KPUD_ODIRECT") == "1":
        size = os.path.getsize(path)
        try:
            return _read_odirect(path, size)
        except Exception as e:
            log(f"ODIRECT failed {path}: {e!r}")
    with open(path, "rb") as f:
        data = f.read()
        try:
            os.posix_fadvise(f.fileno(), 0, 0, os.POSIX_FADV_DONTNEED)
        except (AttributeError, OSError):
            pass
    return data


def _read_odirect(path, size):
    for blk in (4096, 512):
        n_al = (size // blk) * blk
        if n_al == 0:
            continue
        fd = os.open(path, os.O_RDONLY | getattr(os, "O_DIRECT", 0))
        try:
            buf = mmap.mmap(-1, n_al)
            mv = memoryview(buf)
            off = 0
            while off < n_al:
                n = os.preadv(fd, [mv[off:]], off)
                if n <= 0:
                    break
                off += n
            tail = b""
            if size > n_al:
                tail = os.pread(fd, size - n_al, n_al)
            if off < n_al:
                raise ValueError("short read")
            return bytes(buf[:off]) + tail
        finally:
            os.close(fd)
    raise ValueError("empty")


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
        # 2026-09-09 定案：s4_sq 与 重导出的 s16b（mk_sq16b.py）同为 era-B
        # （kmodel 内 W*s，fold 除法 x/s）。老 s16_sq 是 era-A（W/s + x*s）
        # 已弃用。selftest 探针回灌对域盲，不能当方向判据。
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
        # 静态预载：启动时把所有需要的解释器全部加载进 CMA，之后零动态加载。
        # 实测（s16b, drop_caches 后）：196 × 2.29MB = 450MB，21s 加载完。
        # 注意：必须先 drop_caches——页缓存会挤进 CMA 区不迁走，320MB 假占用
        # 曾经把可用池压到 576MB，导致全量驻留误判为"内存不够"。
        #
        # 每个 interpreter 还要做一次 warm-run（零输入跑一遍）：
        # 1) vendor runtime 的首跑会懒分配运行期缓冲（dmesg 实录：GNNE 驱动
        #    在 run 期做 order:10 GFP_KERNEL 连续 4MB 分配，llm 的 700MB 页
        #    缓存把普通区碎片化后分配失败 -> runtime 不查错误直接段错误）。
        #    在内存最干净的窗口把这些分配全部逼出来。
        # 2) 开机即全量验证 196 个 kmodel，坏导出在启动时暴露而不是对话中途。
        import gc
        t0 = time.monotonic()
        for S in (16, 4, 1):
            if CAPS[S] <= 0:
                continue
            n = ok = 0
            for li in range(28):
                for tag in ("q", "k", "v", "o", "gate", "up", "down"):
                    stem = f"l{li}_{tag}"
                    it = get_interp(stem, S)
                    if it is None:
                        continue
                    n += 1
                    try:
                        # 逐个"加载+试跑"交替：读缓存在两次加载之间被
                        # fadvise/回收消化，zone 高阶块始终留得住——
                        # 两段式（先全加载再统一试跑）实测必段错误
                        sh = [int(v) for v in it.get_input_shape(0)]
                        zt = np.zeros(sh, np.float32)
                        tt = nn.RuntimeTensor.from_numpy(zt)
                        it.set_input_tensor(0, tt)
                        it.run()
                        ot = it.get_output_tensor(0)
                        oarr = ot.to_numpy()
                        del ot, oarr, tt, zt
                        ok += 1
                    except Exception as e:
                        # 单个坏 kmodel 不拖垮整体：留在表里，真实请求时该
                        # GEMM 客户端会走 CPU 回退（handle 内同样有 try）
                        log(f"WARMRUN FAIL {stem}: {e}")
            log(f"warmed {ok}/{n} S{S} interpreters (load+run) in "
                f"{time.monotonic()-t0:.1f}s CmaFree="
                f"{open('/proc/meminfo').read().split('CmaFree:')[1].split()[0]}kB")
            t0 = time.monotonic()
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
            conn.settimeout(45)   # GNNE 卡死时尽快释放这条连接（客户端 30s 先超时回退）
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
