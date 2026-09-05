#!/usr/bin/env python3
# surgery5.py — C cut v2: also externalize the durations-derived mask closure.
# Stop rule for the C backward walk: matmul OR any node in the forward closure
# of the durations tensors (the mask construction). Dangling inputs then are
# mm_out + the mask tensors the decoder consumes (Part B computes them).
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
DUR_T = '/Squeeze_output_0'
HID = '/encoder/Mul_1_output_0'
MUL1_T = '/Mul_1_output_0'   # pre-squeeze durations x ls ([1,1,N])
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

# A-side: backward from DUR/HID
sideA = set()
stack = [DUR_T, HID]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None or n.name in sideA:
        continue
    sideA.add(n.name)
    stack.extend(n.input)

# mask-side: forward closure from the duration tensors, stopping before
# matmul and before nodes already in sideA
mask_side = set()
stack = [DUR_T, MUL1_T]
while stack:
    t = stack.pop()
    for c in cons.get(t, []):
        if c.name == matmul.name or c.name in sideA or c.name in mask_side:
            continue
        mask_side.add(c.name)
        stack.extend(c.output)
print('mask-side nodes:', len(mask_side))

# C backward with the extended stop rule
inC = set()
dangling = set()
stack = [C_OUT]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None:
        continue
    if n.name == matmul.name or n.name in sideA or n.name in mask_side:
        dangling.add(t)
        continue
    if n.name in inC:
        continue
    inC.add(n.name)
    stack.extend(n.input)
print('C nodes:', len(inC), 'dangling:', sorted(dangling))

mC = onnx.load(SRC)
keepC = [n for n in mC.graph.node if n.name in inC]
used = {i for n in keepC for i in n.input}
initsC = [i for i in mC.graph.initializer if i.name in used]
del mC.graph.node[:]
del mC.graph.initializer[:]
del mC.graph.output[:]
del mC.graph.input[:]
mC.graph.node.extend(keepC)
mC.graph.initializer.extend(initsC)
for i in onnx.load(SRC).graph.input:
    if i.name in used and i.name not in dangling:
        mC.graph.input.append(i)

# dangling shapes via a probe run of the original graph
want = sorted(dangling)
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
print('dangling shapes:', shapes)

for d in want:
    sh = list(shapes[d])
    # bucketize: replace the runtime dims (non-1 leading kept) — token dim -> XB, frame dim -> MB
    sh = [d if d == 1 else (XB if d <= 64 else MB) if isinstance(d, int) else d for d in sh]
    # heuristic failed for rank-2 [1,N]: patch manually
    if len(sh) == 2 and sh[1] not in (1,):
        sh = [1, XB]
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

pin = {i.name: [d.dim_value for d in i.type.tensor_type.shape.dim]
       for i in mC.graph.input}
sm, ok = simplify(mC, overwrite_input_shapes=pin, skip_fuse_bn=True)
print('C sim ok:', ok, 'nodes:', len(srt), '->', len(sm.graph.node))
onnx.save(sm, '/tmp/matcha-icefall-zh-baker/matcha_C512.onnx')
sC = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C512.onnx',
                          providers=['CPUExecutionProvider'])
print('C inputs:', [(i.name, i.shape) for i in sC.get_inputs()])
print('C outputs:', [(o.name, o.shape) for o in sC.get_outputs()])
