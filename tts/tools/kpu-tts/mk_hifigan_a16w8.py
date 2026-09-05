#!/usr/bin/env python3
# mk_hifigan_qerr.py — diagnostic compile: per-tensor quant error report
# (dump_quant_error), full-bucket calibration. The final DumpTraceInfoPass
# crash is expected and harmless — the report is written during the quant pass.
import os
import glob
import numpy as np
import nncase

SRC = '/tmp/matcha-icefall-zh-baker/hifigan_b512.onnx'
OUT = '/tmp/kpu_poc/matcha_a16w8'
CALIB = '/tmp/kpu_poc/matcha_calib_full'
os.makedirs(OUT, exist_ok=True)

mels = sorted(glob.glob(CALIB + '/mel_*.npy'))
n = len(mels)
print('calibration mels:', n)
samples = [[np.load(p) for p in mels]]

co = nncase.CompileOptions()
co.target = 'k230'
co.quant_type = 'int16'
co.w_quant_type = 'int8'
co.calibrate_method = 'NoClip'
co.dump_ir = True
co.dump_asm = False
co.dump_dir = os.path.join(OUT, 'dump')

compiler = nncase.Compiler(co)
import_options = nncase.ImportOptions()
with open(SRC, 'rb') as f:
    compiler.import_onnx(f.read(), import_options)

ptq = nncase.PTQTensorOptions()
ptq.calibrate_method = 'NoClip'
ptq.quant_type = 'int16'
ptq.w_quant_type = 'int8'
ptq.export_weight_range_by_channel = True
ptq.dump_quant_error = True
ptq.dump_quant_error_symmetric_for_signed = True
ptq.samples_count = n
ptq.set_tensor_data(samples)
compiler.use_ptq(ptq)
compiler.compile()

with open(os.path.join(OUT, 'hifigan_qerr.kmodel'), 'wb') as f:
    compiler.gencode(f)
print('KMODEL ->', os.path.join(OUT, 'hifigan_qerr.kmodel'))
