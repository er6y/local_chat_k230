#!/usr/bin/env python3
# mk_C128_k230.py — compile Part C (windowed decoder, 1472 nodes) a16w8.
import os
import glob
import numpy as np
import nncase

SRC = '/tmp/matcha-icefall-zh-baker/matcha_C128.onnx'
OUT = '/tmp/kpu_poc/matcha_AB'
CALIB = '/tmp/kpu_poc/matcha_calC'
os.makedirs(OUT, exist_ok=True)

mms = sorted(glob.glob(CALIB + '/mm_*.npy'))
mks = sorted(glob.glob(CALIB + '/mk_*.npy'))
dus = sorted(glob.glob(CALIB + '/du_*.npy'))
n = len(mms)
print('C calib windows:', n)
samples = [
    [np.array([0.667], np.float32) for _ in mms],  # noise_scale
    [np.load(p) for p in mks],   # /Cast_3_output_0 f32 [1,1,128]
    [np.load(p) for p in mms],   # /MatMul_output_0 f32 [1,128,80]
    [np.load(p) for p in dus],   # /Mul_1_output_0 f32 [1,1,256]
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
with open(os.path.join(OUT, 'matcha_C128.kmodel'), 'wb') as f:
    compiler.gencode(f)
print('KMODEL ->', os.path.join(OUT, 'matcha_C128.kmodel'))
