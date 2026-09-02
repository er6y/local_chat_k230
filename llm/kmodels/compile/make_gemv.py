#!/usr/bin/env python3
# make_gemv.py - build conv1x1 GEMV/GEMM kmodel for K230 KPU POC
# model: x[1,IN,S,S] --conv1x1(W[OUT,IN,1,1],b[OUT])--> y[1,OUT,S,S]
#   GEMV: S=1 (M=1)   GEMM: S=8 folds M=64 into spatial dims (weights reused 64x)
# variants:
#   s    : IN=1024, OUT=3072, S=1  (~3MB int8 W)  one qwen3 MLP-ish matrix
#   b16  : IN=1024, OUT=16384, S=1 (~16MB) DDR probe
#   b32  : IN=1024, OUT=32768, S=1 (~32MB) deep-DDR probe
#   gemm64: IN=1024, OUT=3072, S=8 (~3MB, M=64) compute ceiling probe
import os, sys
import numpy as np
import torch
import torch.nn as nn

import nncase
import _nncase
import nncase_kpu  # registers 'k230' target


def export_onnx(IN, OUT, S, path):
    torch.manual_seed(42)
    m = nn.Conv2d(IN, OUT, 1, bias=True)
    # small weights so int8 sanity check is meaningful
    with torch.no_grad():
        m.weight.mul_(0.02)
        m.bias.fill_(0.0)
    m.eval()
    x = torch.randn(1, IN, S, S)
    torch.onnx.export(m, x, path, input_names=["x"], output_names=["y"],
                      dynamic_axes=None, opset_version=13, dynamo=False)
    np.save(path + ".w.npy", m.weight.detach().numpy().astype(np.float32))
    np.save(path + ".b.npy", m.bias.detach().numpy().astype(np.float32))
    np.save(path + ".x.npy", x.numpy().astype(np.float32))
    ref = m(x).detach().numpy().astype(np.float32)
    np.save(path + ".y.npy", ref)
    print("onnx exported", path, "ref out shape", ref.shape)


def compile_kmodel(onnx_path, kmodel_path, IN, S):
    compile_options = nncase.CompileOptions()
    compile_options.target = "k230"
    compile_options.quant_type = "int8"     # weights+activations int8
    compile_options.w_quant_type = "int8"
    compile_options.input_type = "int8"
    compile_options.output_type = "int8"
    compile_options.input_layout = "NCHW"
    compile_options.output_layout = "NCHW"
    compile_options.dump_ir = False

    compiler = nncase.Compiler(compile_options)
    import_options = nncase.ImportOptions()
    with open(onnx_path, "rb") as f:
        compiler.import_onnx(f.read(), import_options)
    # ptq calibration: random but scaled like LLM activations (std ~1)
    rng = np.random.default_rng(7)
    n_samples = 8
    calib = [ (rng.standard_normal((1, IN, S, S)) * 1.0).astype(np.float32)
              for _ in range(n_samples) ]
    ptq = nncase.PTQTensorOptions()
    ptq.calibrate_method = "Kld"
    ptq.samples_count = n_samples
    ptq.set_tensor_data([calib])
    compiler.use_ptq(ptq)
    compiler.compile()
    with open(kmodel_path, "wb") as f:
        compiler.gencode(f)
    print("kmodel written", kmodel_path, os.path.getsize(kmodel_path), "bytes")


if __name__ == "__main__":
    which = sys.argv[1] if len(sys.argv) > 1 else "s"
    cfg = {
        "s": (1024, 3072, 1),
        "b16": (1024, 16384, 1),
        "b32": (1024, 32768, 1),
        "gemm64": (1024, 3072, 8),
        "gemm256": (1024, 3072, 16),
    }[which]
    IN, OUT, S = cfg
    tag = f"gemv_{which}_{IN}x{OUT}" + ("" if S == 1 else f"_s{S}")
    os.chdir("/tmp/kpu_poc") if os.path.isdir("/tmp/kpu_poc") else os.makedirs("/tmp/kpu_poc", exist_ok=True)
    export_onnx(IN, OUT, S, f"/tmp/kpu_poc/{tag}.onnx")
    compile_kmodel(f"/tmp/kpu_poc/{tag}.onnx", f"/tmp/kpu_poc/{tag}.kmodel", IN, S)
    print("TAG", tag)
