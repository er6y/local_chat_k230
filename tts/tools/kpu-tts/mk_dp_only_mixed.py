#!/usr/bin/env python3
# mk_dp_only_mixed.py — pass 2: mixed-quant compile of dp_only.
# f32-forced: all non-Conv outputs under flows.{3,5,7} + canvas/* (the
# pivot/onehot/select rewrite is int8-fragile). Convs stay i8.
import os
import json
import numpy as np
import onnx
import nncase

SRC = '/tmp/vits-icefall-zh-aishell3/dp_only.onnx'
OUT = '/tmp/kpu_poc/dp_k230_only'
SCHEME_IN = os.path.join(OUT, 'dump', 'QuantScheme.json')
SCHEME_MIXED = os.path.join(OUT, 'scheme_mixed.json')

m = onnx.load(SRC)
f32_names = set()
for nd in m.graph.node:
    nm = nd.name or ''
    if ('/duration_predictor/flows.' in nm and nd.op_type not in ('Conv',)) or '/canvas/' in nm:
        f32_names.update(nd.output)
print('f32-forced tensor names:', len(f32_names))

s = json.load(open(SCHEME_IN))
changed = 0
missing = set()
for o in s['Outputs']:
    if o['Name'] in f32_names:
        o['DataType'] = 'f32'
        changed += 1
        missing.discard(o['Name'])
    elif o['Name'] in f32_names:
        pass
for nm in f32_names:
    if not any(o['Name'] == nm for o in s['Outputs']):
        missing.add(nm)
print('entries switched to f32:', changed)
print('f32 names missing from scheme (not range-ofd):', len(missing))
json.dump(s, open(SCHEME_MIXED, 'w'), indent=1)
print('scheme written:', SCHEME_MIXED)

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
ptq.quant_scheme = SCHEME_MIXED
compiler.use_ptq(ptq)
compiler.compile()

with open(os.path.join(OUT, 'dp_only_mixed.kmodel'), 'wb') as f:
    compiler.gencode(f)
print('KMODEL ->', os.path.join(OUT, 'dp_only_mixed.kmodel'))
