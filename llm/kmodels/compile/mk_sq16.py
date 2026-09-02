#!/usr/bin/env python3
# make_qwen_kmodels.py - extract qwen3 linear weights from gguf, build int8 conv1x1 kmodels
# GEMMs per layer, SEPARATE kmodel each (llama.cpp ggml graph has independent mul_mat
# nodes per weight, so 1:1 name->kmodel keeps the hook stateless):
#   q/k/v : [2048|1024|1024, 1024]   o: [1024, 2048]
#   gate/up: [3072, 1024]            down: [1024, 3072]
# spatial fold S=16 (M=256) for KPU compute efficiency.
# usage: make_qwen_kmodels.py <layer-specs>   e.g. "0" or "0,1" or "all" [--jobs N]
import os, sys, json, time
import numpy as np
import torch
import torch.nn as nn

sys.path.insert(0, "/mnt/d/work/git_dev/k230_prj/k230_llm/llamacpp/gguf-py")
import gguf
from gguf import GGUFReader

import nncase
import _nncase
import nncase_kpu

GGUF = "/root/qwen3-q4km.gguf"
OUTDIR = "/tmp/kpu_poc/qwen_s16sq"
S = 16
NLAYERS = 28
N_CAL = 4


def get_w(r, name):
    t = [t for t in r.tensors if t.name == name][0]
    # gguf dequantize -> ndarray shaped [ne1, ne0] = [rows=in? cols=out?] we normalize below by meta
    a = gguf.dequantize(t)  # shape (t.shape[1], t.shape[0])? verify empirically
    return a, t.shape  # raw gguf ne order


def gemm_specs(r, li):
    # returns list of (tag, W[out,in]) -- one kmodel per GEMM, 1:1 with ggml weights
    def wmat(name):
        # empirical: dequantize returns ndarray [ne0, ne1] = [out, in] directly (row-major W_math)
        t = [tt for tt in r.tensors if tt.name == name][0]
        a = gguf.dequantize(t.data, t.tensor_type)
        assert a.ndim == 2
        return a

    q = wmat(f"blk.{li}.attn_q.weight")       # [2048,1024]
    k = wmat(f"blk.{li}.attn_k.weight")       # [1024,1024]
    v = wmat(f"blk.{li}.attn_v.weight")       # [1024,1024]
    o = wmat(f"blk.{li}.attn_output.weight")  # [1024,2048]
    g = wmat(f"blk.{li}.ffn_gate.weight")     # [3072,1024]
    u = wmat(f"blk.{li}.ffn_up.weight")       # [3072,1024]
    d = wmat(f"blk.{li}.ffn_down.weight")     # [1024,3072]
    assert q.shape == (2048, 1024) and k.shape == (1024, 1024) and v.shape == (1024, 1024)
    assert o.shape == (1024, 2048) and g.shape == (3072, 1024) and d.shape == (1024, 3072)
    return [("q", q), ("k", k), ("v", v), ("o", o), ("gate", g), ("up", u), ("down", d)]


def calc_smooth_s(tag, IN, S, alpha=0.5):
    # SmoothQuant-style per-in-channel rebalancing from REAL activation dumps:
    # per-tensor int8 input quantization saturates on outlier channels
    # (|x|max ~8 vs typ ~1 -> the one hot channel eats the whole scale).
    # Bake W[:,c] /= s[c] into the kmodel, multiply x[c] *= s[c] at fold time
    # (kpu_gemm reads <tag>.scale). s snapped to powers of two: exponent shift
    # only, zero added rounding on either side.
    amax = np.zeros(IN, dtype=np.float64)
    found = 0
    import glob as _glob
    for p in sorted(_glob.glob(f"/tmp/kpu_poc/calib{S}/{tag}_*.npy")):
        a = np.load(p).astype(np.float32)   # (1,IN,S,S) raw activations
        if a.shape != (1, IN, S, S):
            continue
        ch = np.abs(a[0]).max(axis=(1, 2))
        if not np.isfinite(ch).all():
            continue   # tainted dump (NaN/inf channels): skip the whole file
        amax = np.maximum(amax, ch)
        found += 1
    if not found:
        return None
    # dead (all-zero) channels carry no signal: neutral s=1. A bare 1e-6
    # floor would snap them to ~2^-10 and W/s would blow them up 1024x.
    dead = amax <= 1e-6
    amax = np.maximum(amax, 1e-6)
    s = amax ** alpha
    s = s / float(np.exp(np.mean(np.log(s))))   # normalize: keep overall scale
    s = 2.0 ** np.round(np.log2(s))
    s = np.where(dead, 1.0, s)
    # belt & suspenders: a non-finite scale means NaN weights in the kmodel
    s = np.where(np.isfinite(s) & (s > 0), s, 1.0)
    return s.astype(np.float32)


def build_onnx_and_kmodel(tag, W, outdir, want_probe=True, svec=None):
    OUT, IN = W.shape
    km_pre = f"{outdir}/{tag}.kmodel"
    if os.path.exists(km_pre) and os.path.getsize(km_pre) > 1000:
        # resume support: rebuild probes only for layer 0 if missing
        if want_probe and not os.path.exists(f"{outdir}/{tag}.probe_y.npy"):
            pass  # rare; recompute below would need onnx, just skip probes then
        else:
            print("skip", tag, "(exists)", flush=True)
            return {"tag": tag, "IN": IN, "OUT": OUT, "S": S,
                    "kmodel": km_pre, "size": os.path.getsize(km_pre)}
    torch.manual_seed(42)
    if svec is not None:
        # rebalanced weight: fold-time x*c*s cancels this division exactly
        W = (W / svec[None, :]).astype(np.float32)
        svec.tofile(f"{outdir}/{tag}.scale")
    m = nn.Conv2d(IN, OUT, 1, bias=False)
    with torch.no_grad():
        m.weight.copy_(torch.from_numpy(W.astype(np.float32)).unsqueeze(-1).unsqueeze(-1))
    m.eval()
    x = torch.randn(1, IN, S, S)
    if svec is not None:
        # probes must live in the rebalanced domain too (x*s, W/s: same product)
        x = x * torch.from_numpy(svec).view(1, -1, 1, 1)
    onnx_path = f"{outdir}/{tag}.onnx"
    torch.onnx.export(m, x, onnx_path, input_names=["x"], output_names=["y"],
                      dynamic_axes=None, opset_version=13, dynamo=False)
    if want_probe:
        # ref outputs for probe input (randn) -> board numeric verification
        with torch.no_grad():
            y_ref = m(x).detach().numpy().astype(np.float32)   # [1,OUT,S,S]
        np.save(f"{outdir}/{tag}.probe_x.npy", x.numpy().astype(np.float32))
        np.save(f"{outdir}/{tag}.probe_y.npy", y_ref)

    co = nncase.CompileOptions()
    co.target = "k230"
    co.quant_type = "int8"; co.w_quant_type = "int8"
    co.input_type = "int8"; co.output_type = "int8"
    co.input_layout = "NCHW"; co.output_layout = "NCHW"
    co.dump_ir = False
    compiler = nncase.Compiler(co)
    io = nncase.ImportOptions()
    with open(onnx_path, "rb") as f:
        compiler.import_onnx(f.read(), io)
    rng = np.random.default_rng(7)
    # real-activation calibration (from board KPU_DUMP=ALL) beats randn:
    # LLM activations reach |x|~8+, std-normal calibration saturates int8 input scale
    calib = []
    import glob as _glob
    for p in sorted(_glob.glob(f"/tmp/kpu_poc/calib{S}/{tag}_*.npy")):
        a = np.load(p).astype(np.float32)
        if a.shape == (1, IN, S, S) and bool(np.isfinite(a).all()):
            calib.append(a)   # tainted dumps (NaN) would poison PTQ weights
    if not calib:
        calib = [ (rng.standard_normal((1, IN, S, S)) * 1.0).astype(np.float32) for _ in range(N_CAL) ]
    elif svec is not None:
        # calibrate on the REBALANCED distribution the KPU will actually see
        calib = [ (a * svec.reshape(1, -1, 1, 1)).astype(np.float32) for a in calib ]
    ptq = nncase.PTQTensorOptions()
    ptq.calibrate_method = "NoClip"   # minmax full-range: Kld truncates heavy-tail
    ptq.samples_count = len(calib)     # LLM activations (|x| up to 8+), Kld clips them
    ptq.export_weight_range_by_channel = True  # per-channel w quant: big-activation rows
    ptq.use_mse_quant_w = True         # amplify w-quant error; mse-fit scales reduce it
    ptq.set_tensor_data([calib])
    # NOTE: ptq.quant_scheme is an INPUT (custom scheme json) in nncase 2.11 --
    # leave unset, graph-level auto quant is what we want (f32 in/out via IR probe)
    compiler.use_ptq(ptq)
    compiler.compile()
    km = f"{outdir}/{tag}.kmodel"
    with open(km, "wb") as f:
        compiler.gencode(f)
    os.remove(onnx_path)  # save disk: onnx consumed by compiler, safe to drop now
    print("built", km, os.path.getsize(km), "bytes", "OUT,IN =", OUT, IN, flush=True)
    return {"tag": tag, "IN": IN, "OUT": OUT, "S": S,
            "kmodel": km, "size": os.path.getsize(km)}


# ---- multiprocess plumbing (spawn: torch OpenMP + fork is fragile) ----
_R = None
def _init_worker():
    global _R
    _R = GGUFReader(GGUF)

def build_layer(li):
    entries = []
    for tag, W in gemm_specs(_R, li):
        # probes only needed for layer 0 verification
        sv = calc_smooth_s(f"l{li}_{tag}", W.shape[1], S)
        entries.append(build_onnx_and_kmodel(f"l{li}_{tag}", W, OUTDIR,
                                             want_probe=(li == 0), svec=sv))
    return entries


if __name__ == "__main__":
    spec = sys.argv[1] if len(sys.argv) > 1 else "0"
    jobs = int(sys.argv[sys.argv.index("--jobs") + 1]) if "--jobs" in sys.argv else 1
    os.makedirs(OUTDIR, exist_ok=True)
    layers = list(range(NLAYERS)) if spec == "all" else [int(x) for x in spec.split(",")]
    t_start = time.time()
    if jobs > 1:
        import multiprocessing as mp
        from concurrent.futures import ProcessPoolExecutor
        ctx = mp.get_context("spawn")
        with ProcessPoolExecutor(max_workers=jobs, mp_context=ctx,
                                 initializer=_init_worker) as ex:
            results = list(ex.map(build_layer, layers))
    else:
        _init_worker()
        results = [build_layer(li) for li in layers]
    manifest = [e for sub in results for e in sub]
    with open(f"{OUTDIR}/manifest.json", "w") as f:
        json.dump(manifest, f, indent=1)
    total = sum(e["size"] for e in manifest)
    print(f"manifest entries: {len(manifest)}, total {total/1e6:.1f} MB, "
          f"{time.time()-t_start:.0f}s elapsed")
