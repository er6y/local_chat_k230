#!/usr/bin/env python3
# mk_gnne_repro.py — GNNE int8 真机数值 bug 最小复现生成器
# 生成 6 个 1x1 Conv2d int8 kmodel（k230 target）+ 固定输入 + float32 参考：
#   m_a  IN=64   OUT=64    int8/per-channel（最小）
#   m_b  IN=256  OUT=256   int8/per-channel
#   m_c  IN=1024 OUT=2048  int8/per-channel（与 l0_q 同形状）
#   m_d  IN=1024 OUT=2048  int8/权重 per-tensor（二分 per-channel 权重量化）
#   m_e  IN=1024 OUT=2048  float32 IO（二分 IO 量化）
#   m_f  IN=1024 OUT=2048  int8 + smoothquant svec（完整生产同款路径）
# 输出 outdir: {stem}.kmodel [.scale] {stem}.xin.npy {stem}.ref.npy
import os, sys
import numpy as np
import torch, torch.nn as nn

OUTDIR = sys.argv[1] if len(sys.argv) > 1 else "/tmp/kpu_poc/gnne_repro"
S, MT = 16, 256
os.makedirs(OUTDIR, exist_ok=True)

def build(stem, IN, OUT, seed, per_channel_w=True, float_io=False, svec=None):
    rng = np.random.default_rng(seed)
    W = (rng.standard_normal((OUT, IN)) * 0.05).astype(np.float32)
    x = (rng.standard_normal((1, IN, S, S)) * 1.0).astype(np.float32)
    Wb = W if svec is None else (W * svec[None, :]).astype(np.float32)
    xb = x if svec is None else (x / svec.reshape(1, -1, 1, 1)).astype(np.float32)
    m = nn.Conv2d(IN, OUT, 1, bias=False)
    with torch.no_grad():
        m.weight.copy_(torch.from_numpy(Wb).unsqueeze(-1).unsqueeze(-1))
    m.eval()
    xt = torch.from_numpy(xb)
    with torch.no_grad():
        y_ref = m(xt).detach().numpy().astype(np.float32)   # = W @ x（域自消）

    onnx = f"{OUTDIR}/{stem}.onnx"
    torch.onnx.export(m, xt, onnx, input_names=["x"], output_names=["y"],
                      opset_version=13, dynamo=False)

    import nncase
    co = nncase.CompileOptions()
    co.target = "k230"
    co.quant_type = "int8"; co.w_quant_type = "int8"
    if float_io:
        co.input_type = "float32"; co.output_type = "float32"
    else:
        co.input_type = "int8"; co.output_type = "int8"
    co.input_layout = "NCHW"; co.output_layout = "NCHW"
    compiler = nncase.Compiler(co)
    with open(onnx, "rb") as f:
        compiler.import_onnx(f.read(), nncase.ImportOptions())
    ptq = nncase.PTQTensorOptions()
    ptq.calibrate_method = "NoClip"
    calib = [xb] * 4
    ptq.samples_count = len(calib)
    ptq.export_weight_range_by_channel = per_channel_w
    ptq.use_mse_quant_w = True
    ptq.set_tensor_data([calib])
    compiler.use_ptq(ptq)
    compiler.compile()
    with open(f"{OUTDIR}/{stem}.kmodel", "wb") as f:
        f.write(compiler.gencode_tobytes())
    if svec is not None:
        svec.tofile(f"{OUTDIR}/{stem}.scale")
    # 客户端发 raw 域 x（daemon 对 m_f 会 /s）；参考统一 = W @ x
    np.save(f"{OUTDIR}/{stem}.xin.npy", x[0].reshape(IN, MT).T.copy())   # [256,IN] raw 域
    np.save(f"{OUTDIR}/{stem}.ref.npy", y_ref.reshape(OUT, MT).T.copy())  # [256,OUT]
    os.remove(onnx)
    print(f"built {stem}: IN={IN} OUT={OUT} "
          f"{'float_io' if float_io else 'int8'} "
          f"{'per-tensor-w' if not per_channel_w else 'per-channel-w'} "
          f"{'smoothquant' if svec is not None else 'plain'}", flush=True)

# stem 名沿用生产命名法（l{n}_{q|k|v|o|gate|up|down}）：daemon 的 infer_K
# 按 split("_")[1] 查 SPEC 表，故复现模型借真实后缀映射形状。
#   r0_q   IN=1024 OUT=2048  int8/per-channel（=l0_q 同形状）
#   r1_q   IN=1024 OUT=2048  权重 per-tensor（二分 per-channel 量化）
#   r2_q   IN=1024 OUT=2048  float32 IO（二分 IO 量化）
#   r3_q   IN=1024 OUT=2048  smoothquant svec（完整生产同款路径）
#   r4_o   IN=2048 OUT=1024  尺寸轴（IN=2048）
#   r5_down IN=3072 OUT=1024 尺寸轴（IN=3072）
build("r0_q", 1024, 2048, seed=3)
build("r1_q", 1024, 2048, seed=3, per_channel_w=False)
build("r2_q", 1024, 2048, seed=3, float_io=True)
sv = (2.0 ** np.round(np.random.default_rng(9).standard_normal(1024))).astype(np.float32)
build("r3_q", 1024, 2048, seed=3, svec=sv)
build("r4_o", 2048, 1024, seed=4)
build("r5_down", 3072, 1024, seed=5)
print("REPRO SET READY ->", OUTDIR)
