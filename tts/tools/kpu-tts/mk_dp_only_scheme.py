#!/usr/bin/env python3
# mk_dp_only_scheme.py — compile pass 1: export the quant scheme for dp_only,
# then write a mixed-quant scheme: flows.{3,5,7} non-conv machinery + canvas/*
# stay f32 (the onehot-select/pivot rewrite is int8-fragile at steep histogram
# positions); convs keep int8. Scheme import takes a FILE PATH.
import os
import glob
import numpy as np
import onnx
import nncase
import json

SRC = '/tmp/vits-icefall-zh-aishell3/dp_only.onnx'
OUT = '/tmp/kpu_poc/dp_k230_only'
CALIB = '/tmp/kpu_poc/dpx_calib'
DUMP = os.path.join(OUT, 'dump')
os.makedirs(DUMP, exist_ok=True)

xs = sorted(glob.glob(CALIB + '/x_*.npy'))
tks = sorted(glob.glob(CALIB + '/tok_*.npy'))
tls = sorted(glob.glob(CALIB + '/tl_*.npy'))
sps = sorted(glob.glob(CALIB + '/spk_*.npy'))
n = len(xs)
samples = [[np.load(p) for p in xs],
           [np.load(p).astype(np.int64) for p in tks],
           [np.load(p).astype(np.int64) for p in tls],
           [np.load(p).astype(np.int64) for p in sps],
           [np.array([0.8], np.float32) for _ in xs],
           [np.array([1.0], np.float32) for _ in xs]]

# tensors to keep in f32: every non-Conv node output under flows.{3,5,7}
# (histogram chains, pivot/onehot selects, quartic tree, canvas machinery)
m = onnx.load(SRC)
f32_names = set()
for nd in m.graph.node:
    nm = nd.name or ''
    if ('/duration_predictor/flows.' in nm and nd.op_type not in ('Conv',)) or '/canvas/' in nm:
        f32_names.update(nd.output)
print('f32-forced tensor count:', len(f32_names))

co = nncase.CompileOptions()
co.target = 'k230'
co.quant_type = 'int8'
co.w_quant_type = 'int8'
co.calibrate_method = 'NoClip'
co.dump_ir = True      # scheme export writes into the dump dir
co.dump_asm = False
co.dump_dir = DUMP

compiler = nncase.Compiler(co)
import_options = nncase.ImportOptions()
with open(SRC, 'rb') as f:
    compiler.import_onnx(f.read(), import_options)

ptq = nncase.PTQTensorOptions()
ptq.calibrate_method = 'NoClip'
ptq.quant_type = 'int8'
ptq.w_quant_type = 'int8'
ptq.export_weight_range_by_channel = False  # by-tensor scheme entries
ptq.export_quant_scheme = True
ptq.samples_count = n
ptq.set_tensor_data(samples)
compiler.use_ptq(ptq)

# The compile will crash in DumpTraceInfoPass (Erf metric, 2.11 bug) AFTER the
# scheme is written; catch and continue.
try:
    compiler.compile()
    print('compile finished (unexpected but fine)')
except Exception as e:
    print('compile raised (expected if DumpTraceInfoPass):', str(e)[:120])

# find the exported scheme
cands = glob.glob(os.path.join(DUMP, '**', 'QuantScheme.json'), recursive=True)
cands += glob.glob(os.path.join(OUT, '**', 'QuantScheme.json'), recursive=True)
print('scheme candidates:', cands)
