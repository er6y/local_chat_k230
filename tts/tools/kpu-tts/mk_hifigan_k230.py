#!/usr/bin/env python3
# mk_hifigan_k230.py — compile the static hifigan (mel[1,80,512] -> audio)
# to a k230 kmodel, int8 PTQ with the real-sentence mel calibration set.
import os
import glob
import numpy as np
import nncase

SRC = '/tmp/matcha-icefall-zh-baker/hifigan_b512.onnx'
OUT = '/tmp/kpu_poc/matcha_k230'
CALIB = '/tmp/kpu_poc/matcha_calib'
os.makedirs(OUT, exist_ok=True)

mels = sorted(glob.glob(CALIB + '/mel_*.npy'))
n = len(mels)
print('calibration mels:', n)
samples = [[np.load(p) for p in mels]]  # param-major: 1 input

co = nncase.CompileOptions()
co.target = 'k230'
co.quant_type = 'int8'
co.w_quant_type = 'int8'
co.calibrate_method = 'NoClip'
co.dump_ir = False
co.dump_asm = False

compiler = nncase.Compiler(co)
import_options = nncase.ImportOptions()
with open(SRC, 'rb') as f:
    compiler.import_onnx(f.read(), import_options)

ptq = nncase.PTQTensorOptions()
ptq.calibrate_method = 'NoClip'
ptq.quant_type = 'int8'
ptq.w_quant_type = 'int8'
ptq.export_weight_range_by_channel = True
ptq.samples_count = n
ptq.set_tensor_data(samples)
compiler.use_ptq(ptq)
compiler.compile()

with open(os.path.join(OUT, 'hifigan_b512.kmodel'), 'wb') as f:
    compiler.gencode(f)
print('KMODEL ->', os.path.join(OUT, 'hifigan_b512.kmodel'))
