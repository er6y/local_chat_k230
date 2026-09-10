#!/usr/bin/env python3
# mk_sq16b.py — S16 era-B 导出（与验证正确的 s4_sq 同方向）
#   kmodel 烤 W*s，运行时 fold 除法 x/s（daemon handle() 的 S4 分支）
# 老的 s16_sq 是 era-A（W/s + x*s），实测乱码根因之一；此脚本重导正确方向。
# 基于 mk_sq4.py，改动：S=16、W*s、probe/calib 域翻转、calib16_clean。
import os, sys, json, time
import numpy as np
import torch
import torch.nn as nn

sys.path.insert(0, "/mnt/d/work/git_dev/k230_prj/local_chat_k230/llm/llamacpp/gguf-py")
import gguf
from gguf import GGUFReader

import nncase
import _nncase
import nncase_kpu

GGUF = "/root/qwen3-q4km.gguf"
OUTDIR = os.environ.get("KPU_OUTDIR", "/tmp/kpu_poc/qwen_s16sqb")
S = 16
CALIB_DIR = f"/tmp/kpu_poc/calib16_clean"   # 干净校准（CPU 路径原始激活）
NLAYERS = 28
N_CAL = 4


def get_w(r, name):
    t = [t for t in r.tensors if t.name == name][0]
    a = gguf.dequantize(t.data, t.tensor_type)
    assert a.ndim == 2
    return a


def gemm_specs(r, li):
    q = get_w(r, f"blk.{li}.attn_q.weight")
    k = get_w(r, f"blk.{li}.attn_k.weight")
    v = get_w(r, f"blk.{li}.attn_v.weight")
    o = get_w(r, f"blk.{li}.attn_output.weight")
    g = get_w(r, f"blk.{li}.ffn_gate.weight")
    u = get_w(r, f"blk.{li}.ffn_up.weight")
    d = get_w(r, f"blk.{li}.ffn_down.weight")
    return [("q", q), ("k", k), ("v", v), ("o", o), ("gate", g), ("up", u), ("down", d)]


def calc_smooth_s(tag, IN, alpha=0.5):
    amax = np.zeros(IN, dtype=np.float64)
    found = 0
    import glob as _glob
    for p in sorted(_glob.glob(f"{CALIB_DIR}/{tag}_*.npy")):
        a = np.load(p).astype(np.float32)
        if a.shape != (1, IN, S, S):
            continue
        ch = np.abs(a[0]).max(axis=(1, 2))
        if not np.isfinite(ch).all():
            continue
        amax = np.maximum(amax, ch)
        found += 1
    if not found:
        return None
    dead = amax <= 1e-6
    amax = np.maximum(amax, 1e-6)
    s = amax ** alpha
    s = s / float(np.exp(np.mean(np.log(s))))
    s = 2.0 ** np.round(np.log2(s))
    s = np.where(dead, 1.0, s)
    s = np.where(np.isfinite(s) & (s > 0), s, 1.0)
    return s.astype(np.float32)


def build_onnx_and_kmodel(tag, W, outdir, want_probe=True, svec=None):
    OUT, IN = W.shape
    km_pre = f"{outdir}/{tag}.kmodel"
    if os.path.exists(km_pre) and os.path.getsize(km_pre) > 1000:
        print("skip", tag, "(exists)", flush=True)
        return {"tag": tag, "IN": IN, "OUT": OUT, "S": S,
                "kmodel": km_pre, "size": os.path.getsize(km_pre)}
    torch.manual_seed(42)
    if svec is not None:
        # era-B：kmodel 烤 W*s，fold 端除法 x/s（生产 daemon 的 S4 分支同款）
        W = (W * svec[None, :]).astype(np.float32)
        svec.tofile(f"{outdir}/{tag}.scale")
    m = nn.Conv2d(IN, OUT, 1, bias=False)
    with torch.no_grad():
        m.weight.copy_(torch.from_numpy(W.astype(np.float32)).unsqueeze(-1).unsqueeze(-1))
    m.eval()
    x = torch.randn(1, IN, S, S)
    if svec is not None:
        # 探针落在 rebalanced 域（randn/s）：selftest 回灌 *s 后生产 /s 还原
        x = x / torch.from_numpy(svec).view(1, -1, 1, 1)
    onnx_path = f"{outdir}/{tag}.onnx"
    torch.onnx.export(m, x, onnx_path, input_names=["x"], output_names=["y"],
                      dynamic_axes=None, opset_version=13, dynamo=False)
    if want_probe:
        with torch.no_grad():
            y_ref = m(x).detach().numpy().astype(np.float32)
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
    calib = []
    import glob as _glob
    for p in sorted(_glob.glob(f"{CALIB_DIR}/{tag}_*.npy")):
        a = np.load(p).astype(np.float32)
        if a.shape == (1, IN, S, S) and bool(np.isfinite(a).all()):
            calib.append(a)
    if not calib:
        calib = [(rng.standard_normal((1, IN, S, S)) * 1.0).astype(np.float32)
                 for _ in range(N_CAL)]
    elif svec is not None:
        # era-B：校准 rebalanced 分布（/s）——KPU 实际看到的域
        calib = [(a / svec.reshape(1, -1, 1, 1)).astype(np.float32) for a in calib]
    ptq = nncase.PTQTensorOptions()
    ptq.calibrate_method = "NoClip"
    ptq.samples_count = len(calib)
    ptq.export_weight_range_by_channel = True
    ptq.use_mse_quant_w = True
    ptq.set_tensor_data([calib])
    compiler.use_ptq(ptq)
    compiler.compile()
    km = f"{outdir}/{tag}.kmodel"
    with open(km, "wb") as f:
        compiler.gencode(f)
    os.remove(onnx_path)
    print("built", km, os.path.getsize(km), "bytes", "OUT,IN =", OUT, IN, flush=True)
    return {"tag": tag, "IN": IN, "OUT": OUT, "S": S,
            "kmodel": km, "size": os.path.getsize(km)}


_R = None
def _init_worker():
    global _R
    _R = GGUFReader(GGUF)


def build_layer(li):
    entries = []
    no_sq = bool(os.environ.get("KPU_NO_SQ"))   # 2026-09-10：SmoothQuant 走
    # k230 per-tensor 输入量化必然坏（真机 17% 误差，见 repro）；置 KPU_NO_SQ
    # 导出无 svec 的纯 int8 版（daemon sc=None 不做 /s，真机 1.3% 正常误差）
    for tag, W in gemm_specs(_R, li):
        sv = None if no_sq else calc_smooth_s(f"l{li}_{tag}", W.shape[1])
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
