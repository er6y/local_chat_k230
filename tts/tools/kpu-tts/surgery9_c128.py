#!/usr/bin/env python3
# surgery9_c128.py — Part C at EXACT 128-frame windows (no padding, no mask
# corruption): same cut, pinned [1,128,80]; verify C128 == original-graph
# run at exactly L'=128 (graph-exactness; windowing is the design choice).
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
ENTRY0 = '/Cast_3_output_0'
DUR = '/Squeeze_output_0'
HID = '/encoder/Mul_1_output_0'
XB, WB = 256, 128   # token bucket, window frames

m = onnx.load(SRC)
G = m.graph
prod = {}
cons = {}
for n in G.node:
    for o in n.output:
        prod[o] = n
    for i in n.input:
        cons.setdefault(i, []).append(n)
matmul = next(n for n in G.node if n.op_type == 'MatMul' and '/Transpose_1_output_0' in n.input)

sideA = set()
stack = [DUR, HID]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None or n.name in sideA:
        continue
    sideA.add(n.name)
    stack.extend(n.input)

inC = set()
dangle = set()
stack = ['mel']
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None:
        continue
    if n.name == matmul.name or n.name in sideA:
        dangle.add(t)
        continue
    if n.name in inC:
        continue
    inC.add(n.name)
    stack.extend(n.input)

maskC = set()
stack = [ENTRY0, '/Unsqueeze_11_output_0']
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None or n.name in maskC or n.name == matmul.name or n.name in sideA:
        continue
    if n.name not in inC:
        continue
    maskC.add(n.name)
    stack.extend(n.input)
finalC = inC - maskC

mC = onnx.load(SRC)
slice2 = next(n for n in mC.graph.node if n.name == '/Slice_2')
keep = []
for n in mC.graph.node:
    if n.name not in finalC:
        continue
    if n.name == '/Slice_2':
        keep.append(onnx.helper.make_node('Identity', [n.input[0]], list(n.output),
                                          name='/Slice_2_identity'))
    else:
        keep.append(n)
used = {i for n in keep for i in n.input}
inits = [i for i in mC.graph.initializer if i.name in used]
del mC.graph.node[:]
del mC.graph.initializer[:]
del mC.graph.output[:]
del mC.graph.input[:]
mC.graph.node.extend(keep)
mC.graph.initializer.extend(inits)
for i in onnx.load(SRC).graph.input:
    if i.name in used and i.name not in dangle:
        mC.graph.input.append(i)

# dangles + entries as inputs
entries = set()
for n in G.node:
    if n.name in maskC or n.name not in inC:
        continue
    for i in n.input:
        p = prod.get(i)
        if p is not None and p.name in maskC:
            entries.add(i)
for d in sorted(dangle | entries):
    if d == '/Unsqueeze_11_output_0':
        continue
    sh = [1, WB, 80] if d == matmul.output[0] else (
        [1, 1, WB] if d == ENTRY0 else [1, 1, XB])
    mC.graph.input.append(onnx.helper.make_tensor_value_info(d, onnx.TensorProto.FLOAT, sh))
mC.graph.output.append(onnx.helper.make_tensor_value_info(
    'mel', onnx.TensorProto.FLOAT, [1, 80, WB]))

produced = set(i.name for i in mC.graph.initializer)
produced.update(i.name for i in mC.graph.input)
rem = list(mC.graph.node)
srt = []
while rem:
    for n in list(rem):
        if all(i in produced or i == '' for i in n.input):
            srt.append(n)
            rem.remove(n)
            produced.update(n.output)
            break
    else:
        raise RuntimeError('unsat ' + rem[0].name)
del mC.graph.node[:]
mC.graph.node.extend(srt)
pin = {i.name: [d.dim_value for d in i.type.tensor_type.shape.dim] for i in mC.graph.input}
sm, ok = simplify(mC, overwrite_input_shapes=pin, skip_fuse_bn=True)
print('C128 sim ok:', ok, len(srt), '->', len(sm.graph.node))
onnx.save(sm, '/tmp/matcha-icefall-zh-baker/matcha_C128.onnx')
sC = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C128.onnx',
                          providers=['CPUExecutionProvider'])
print('C128 inputs:', [(i.name, i.shape) for i in sC.get_inputs()])

# ---- verify: original graph run at EXACTLY L'=128 vs C128 ----
# construct tokens whose durations sum to exactly 128? hard. instead pick a
# sentence, run original with length_scale tuned so Lp=128; simpler: run the
# original with a token set, get Lp; then WINDOW its mm into 128-frame pieces
# and C128 each piece; compare C128(window) vs C-dyn(window) with the same
# window inputs (graph equivalence at exact length).
sCd = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C_dyn.onnx',
                           providers=['CPUExecutionProvider'])
rng = np.random.default_rng(3)
tok = rng.integers(4, 2000, 10).astype(np.int64)
ids = np.asarray([1] + [v for t in tok for v in (int(t), 1)], np.int64)

m2 = onnx.load(SRC)
for w in ['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0']:
    m2.graph.output.append(onnx.helper.make_tensor_value_info(w, onnx.TensorProto.FLOAT, None))
s2 = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
mm_o, c3_o, dur_o = s2.run(
    ['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0'],
    {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
     'noise_scale': np.array([0.0], np.float32),
     'length_scale': np.array([1.0], np.float32)})
Lp = mm_o.shape[1]
print('Lp', Lp)

# take the first exact-128 window of this utterance
W = mm_o[:, :WB, :]
M = c3_o[:, :, :WB]
D = dur_o
mel_w_dyn = sCd.run(None, {'/MatMul_output_0': W, '/Cast_3_output_0': M,
                           '/Mul_1_output_0': D,
                           'noise_scale': np.array([0.0], np.float32)})[0]
Wb = np.zeros((1, WB, 80), np.float32)
Wb[0] = W[0]
Mb = np.ones((1, 1, WB), np.float32)
Db = np.zeros((1, 1, XB), np.float32)
Db[0, 0, :D.shape[2]] = D[0]
mel_w_128 = sC.run(None, {'/MatMul_output_0': Wb, '/Cast_3_output_0': Mb,
                          '/Mul_1_output_0': Db,
                          'noise_scale': np.array([0.0], np.float32)})[0]
d = np.abs(mel_w_128 - mel_w_dyn)
print(f'C128 vs C-dyn @exact-128 window: maxdiff={d.max():.6f} mean={d.mean():.6f}')
