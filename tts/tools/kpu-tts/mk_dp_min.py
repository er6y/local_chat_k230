#!/usr/bin/env python3
# mk_dp_min.py — minimal mixed scheme: force f32 ONLY on value-sensitive ops
# (comparisons, cumsum, softmax, divisions, canvas quartic/select machinery);
# keep layout ops (Transpose/Reshape/Slice/Concat/Pad) and arithmetic in i8
# to preserve KPU conv fusion. Goal: int8 speed with mixed accuracy.
import os
import json
import numpy as np
import onnx
import nncase

SRC = '/tmp/vits-icefall-zh-aishell3/dp_only.onnx'
OUT = '/tmp/kpu_poc/dp_k230_only'
SCHEME_IN = os.path.join(OUT, 'dump', 'QuantScheme.json')
SCHEME_MIN = os.path.join(OUT, 'scheme_min.json')

F32_OPS = {'Softmax', 'CumSum', 'Div', 'GreaterOrEqual', 'LessOrEqual',
           'Equal', 'Min', 'Where', 'Softplus', 'Exp', 'RandomNormalLike'}

m = onnx.load(SRC)
f32_names = set()
for nd in m.graph.node:
    nm = nd.name or ''
    if '/canvas/' in nm or nd.op_type in F32_OPS:
        f32_names.update(nd.output)
# tensors consumed by canvas nodes coming from other producers (x, div0/div1...)
cons = {}
for nd in m.graph.node:
    if '/canvas/' in (nd.name or ''):
        for i in nd.input:
            cons.setdefault(i, []).append(nd.name)
for t in cons:
    f32_names.add(t)
print('f32-forced names:', len(f32_names))

s = json.load(open(SCHEME_IN))
have = {o['Name'] for o in s['Outputs']}
changed = 0
for o in s['Outputs']:
    if o['Name'] in f32_names:
        o['DataType'] = 'f32'
        changed += 1
print('switched:', changed, ' canvas-covered:', sum(1 for n in f32_names if n in have))
json.dump(s, open(SCHEME_MIN, 'w'), indent=1)

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
ptq.quant_scheme = SCHEME_MIN
compiler.use_ptq(ptq)
compiler.compile()

with open(os.path.join(OUT, 'dp_only_min.kmodel'), 'wb') as f:
    compiler.gencode(f)
print('KMODEL ->', os.path.join(OUT, 'dp_only_min.kmodel'))
