#!/usr/bin/env python3
# mk_C_m.py — compile the 3-input C128m (mm/mask/noise) a16w8.
import os, glob
import numpy as np
import nncase

SRC = '/tmp/matcha-icefall-zh-baker/matcha_C128m.onnx'
OUT = '/tmp/kpu_poc/matcha_AB'
CALIB = '/tmp/kpu_poc/matcha_calC'

mms = sorted(glob.glob(CALIB + '/mm_*.npy'))
mks = sorted(glob.glob(CALIB + '/mk_*.npy'))
n = len(mms)
print('calib windows:', n)
g = np.random.default_rng(3)
noise_calib = [ (g.normal(0, 1, (1, 80, 128)) * 0.667).astype(np.float32) for _ in mms ]
samples = [
    [np.load(p) for p in mms],
    [np.load(p) for p in mks],
    noise_calib,
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
print('KMODEL -> matcha_C128.kmodel (a16w8, 3-input noise-cut)')
