#!/usr/bin/env python3
# try_parts_q.py — compile a fragment with a16w8 PTQ (calib windows subset).
import sys
import glob
import os
import numpy as np
import nncase

path = sys.argv[1]
out = sys.argv[2]

CALIB = '/tmp/kpu_poc/matcha_calC'
mms = sorted(glob.glob(CALIB + '/mm_*.npy'))
mks = sorted(glob.glob(CALIB + '/mk_*.npy'))
dus = sorted(glob.glob(CALIB + '/du_*.npy'))
# subset for speed: every 4th window
mms, mks, dus = mms[::4], mks[::4], dus[::4]
n = len(mms)
samples = [
    [np.array([0.667], np.float32) for _ in mms],
    [np.load(p) for p in mks],
    [np.load(p) for p in mms],
    [np.load(p) for p in dus],
]

co = nncase.CompileOptions()
co.target = 'k230'
co.quant_type = 'int16'
co.w_quant_type = 'int8'
co.calibrate_method = 'NoClip'
co.dump_ir = False
co.dump_asm = False
compiler = nncase.Compiler(co)
with open(path, 'rb') as f:
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
with open(out, 'wb') as f:
    compiler.gencode(f)
print('OK ->', out)
