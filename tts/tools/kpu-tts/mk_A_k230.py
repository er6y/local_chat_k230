#!/usr/bin/env python3
# mk_A_k230.py — compile Part A (encoder+DP, 498 nodes) to k230 kmodel, a16w8.
import os
import glob
import numpy as np
import nncase

SRC = '/tmp/matcha-icefall-zh-baker/matcha_A256.onnx'
OUT = '/tmp/kpu_poc/matcha_AB'
CALIB = '/tmp/kpu_poc/matcha_calA'
os.makedirs(OUT, exist_ok=True)

xs = sorted(glob.glob(CALIB + '/x_*.npy'))
n = len(xs)
print('A calib:', n)
# inputs x[1,256] i64, x_length[1] i64, length_scale[1] f32 — param-major
samples = [
    [np.load(p) for p in xs],
    [np.array([256], np.int64) for _ in xs],
    [np.array([1.0], np.float32) for _ in xs],
]

co = nncase.CompileOptions()
co.target = 'k230'
co.quant_type = 'int16'
co.w_quant_type = 'int8'
co.calibrate_method = 'NoClip'
co.dump_ir = False
co.dump_asm = False
compiler = nncase.Compiler(co)
with open(SRC, 'rb') as f:
    compiler.import_onnx(f.read(), nncase.ImportOptions())
ptq = nncase.PTQTensorOptions()
ptq.calibrate_method = 'NoClip'
ptq.quant_type = 'int16'
ptq.w_quant_type = 'int8'
ptq.export_weight_range_by_channel = True
ptq.samples_count = n
ptq.set_tensor_data(samples)
compiler.use_ptq(ptq)
compiler.compile()
with open(os.path.join(OUT, 'matcha_A.kmodel'), 'wb') as f:
    compiler.gencode(f)
print('KMODEL ->', os.path.join(OUT, 'matcha_A.kmodel'))
