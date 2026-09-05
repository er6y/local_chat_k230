#!/usr/bin/env python3
# surgery6.py — C v3: from the surgery4 C graph, excise the internal mask
# construction (forward closure of the durations input) and expose the exact
# entry tensors it feeds the decoder with as C inputs.
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
DUR_T = '/Squeeze_output_0'
HID = '/encoder/Mul_1_output_0'
MUL1_T = '/Mul_1_output_0'
XB, MB = 256, 512

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
C_OUT = next(n for n in G.node if n.name == '/Slice_2').input[0]

# A-side (encoder+DP backward closure)
sideA = set()
stack = [DUR_T, HID]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None or n.name in sideA:
        continue
    sideA.add(n.name)
    stack.extend(n.input)

# C-side backward stopping ONLY at {matmul, sideA}
inC = set()
dangle1 = set()
stack = [C_OUT]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None:
        continue
    if n.name == matmul.name or n.name in sideA:
        dangle1.add(t)
        continue
    if n.name in inC:
        continue
    inC.add(n.name)
    stack.extend(n.input)
print('C v1 nodes:', len(inC), 'dangle1:', sorted(dangle1))

# inside C: mask construction = BACKWARD closure of the known mask entry
# (/Cast_3_output_0, seen in the estimator_2 bridge), bounded by sideA/matmul
ENTRY0 = '/Cast_3_output_0'
maskC = set()
stack = [ENTRY0]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None or n.name in maskC or n.name == matmul.name or n.name in sideA:
        continue
    if n.name not in inC:
        continue
    maskC.add(n.name)
    stack.extend(n.input)
print('internal mask construction nodes:', len(maskC))

# entry tensors: produced by maskC, consumed by C \ maskC
entry = set()
for n in G.node:
    if n.name in maskC or n.name not in inC:
        continue
    for i in n.input:
        p = prod.get(i)
        if p is not None and p.name in maskC:
            entry.add(i)
print('mask entry tensors:', sorted(entry))

# final C = inC \ maskC
finalC = inC - maskC
mC = onnx.load(SRC)
keepC = [n for n in mC.graph.node if n.name in finalC]
used = {i for n in keepC for i in n.input}
initsC = [i for i in mC.graph.initializer if i.name in used]
del mC.graph.node[:]
del mC.graph.initializer[:]
del mC.graph.output[:]
del mC.graph.input[:]
mC.graph.node.extend(keepC)
mC.graph.initializer.extend(initsC)
for i in onnx.load(SRC).graph.input:
    if i.name in used and i.name not in dangle1 and i.name not in entry:
        mC.graph.input.append(i)

# shapes of dangle1 + entry via probe on the original
want = sorted(dangle1 | entry)
m2 = onnx.load(SRC)
for w in want:
    m2.graph.output.append(onnx.helper.make_tensor_value_info(w, onnx.TensorProto.FLOAT, None))
s2 = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
tok0 = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
ids = np.asarray([1] + [v for t in tok0 for v in (t, 1)], np.int64)
outs = s2.run(want, {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
                     'noise_scale': np.array([0.0], np.float32),
                     'length_scale': np.array([1.0], np.float32)})
shapes = {w: o.shape for w, o in zip(want, outs)}
print('shapes:', shapes)

# pin dims: frame-ish dim -> MB, token-ish -> XB
def bucketize(sh):
    out = []
    for d in sh:
        if d == 1:
            out.append(1)
        elif d == 80:
            out.append(80)
        elif d == len(ids):          # token count
            out.append(XB)
        else:                        # frame-ish (204) or channel
            out.append(MB if d > 64 else XB)
    return out

for d in want:
    sh = bucketize(shapes[d])
    if d == matmul.output[0]:
        sh = [1, MB, 80]
    mC.graph.input.append(onnx.helper.make_tensor_value_info(
        d, onnx.TensorProto.FLOAT, sh))
mC.graph.output.append(onnx.helper.make_tensor_value_info(
    C_OUT, onnx.TensorProto.FLOAT, [1, 80, MB]))

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
        raise RuntimeError('unsat: ' + rem[0].name)
del mC.graph.node[:]
mC.graph.node.extend(srt)
pin = {i.name: [d.dim_value for d in i.type.tensor_type.shape.dim] for i in mC.graph.input}
sm, ok = simplify(mC, overwrite_input_shapes=pin, skip_fuse_bn=True)
print('C v3 sim ok:', ok, 'nodes:', len(srt), '->', len(sm.graph.node))
onnx.save(sm, '/tmp/matcha-icefall-zh-baker/matcha_C512.onnx')
sC = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C512.onnx',
                          providers=['CPUExecutionProvider'])
print('C inputs:', [(i.name, i.shape) for i in sC.get_inputs()])
print('C outputs:', [(o.name, o.shape) for o in sC.get_outputs()])
