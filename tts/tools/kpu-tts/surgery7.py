#!/usr/bin/env python3
# surgery7.py — C final: cut {matmul, sideA, maskC}, replace the dynamic
# Slice_2 with Identity (runner slices to Lp), keep the output normalization
# (Mul_6/Add) inside C. Verify A+B+C vs original mel end-to-end.
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
ENTRY0 = '/Cast_3_output_0'
DUR = '/Squeeze_output_0'
HID = '/encoder/Mul_1_output_0'
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

sideA = set()
stack = [DUR, HID]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None or n.name in sideA:
        continue
    sideA.add(n.name)
    stack.extend(n.input)

# backward from 'mel', stopping at matmul/sideA; record the mask entry by
# stopping ALSO at the durations-derived mask family: first walk WITHOUT it,
# then excise backward(ENTRY0) as before — but keep Slice_2's post-chain.
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
entries = set()
for n in G.node:
    if n.name in maskC or n.name not in inC:
        continue
    for i in n.input:
        p = prod.get(i)
        if p is not None and p.name in maskC:
            entries.add(i)
print('C nodes:', len(finalC), 'dangle:', sorted(dangle), 'entries:', sorted(entries))

# build the graph, replacing Slice_2 with Identity
mC = onnx.load(SRC)
slice2 = next(n for n in mC.graph.node if n.name == '/Slice_2')
keep = []
for n in mC.graph.node:
    if n.name not in finalC:
        continue
    if n.name == '/Slice_2':
        ident = onnx.helper.make_node('Identity', [n.input[0]], list(n.output),
                                      name='/Slice_2_identity')
        keep.append(ident)
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
    if i.name in used and i.name not in dangle and i.name not in entries:
        mC.graph.input.append(i)

# shapes via original probe
want = sorted(dangle | entries)
INT64_TENSORS = {'/Unsqueeze_11_output_0'}
m2 = onnx.load(SRC)
for w in want:
    et = onnx.TensorProto.INT64 if w in INT64_TENSORS else onnx.TensorProto.FLOAT
    m2.graph.output.append(onnx.helper.make_tensor_value_info(w, et, None))
s2 = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
tok0 = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
ids = np.asarray([1] + [v for t in tok0 for v in (t, 1)], np.int64)
outs = s2.run(want, {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
                     'noise_scale': np.array([0.0], np.float32),
                     'length_scale': np.array([1.0], np.float32)})
shapes = {w: o.shape for w, o in zip(want, outs)}

for d in want:
    if d == '/Unsqueeze_11_output_0':
        continue  # only fed the replaced dynamic slice; runner slices instead
    if d == matmul.output[0]:
        sh = [1, MB, 80]
    elif d == ENTRY0:
        sh = [1, 1, MB]
    else:
        sh = [1, 1, XB]
    mC.graph.input.append(onnx.helper.make_tensor_value_info(d, onnx.TensorProto.FLOAT, sh))
mC.graph.output.append(onnx.helper.make_tensor_value_info(
    'mel', onnx.TensorProto.FLOAT, [1, 80, MB]))

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
print('sim ok:', ok, len(srt), '->', len(sm.graph.node))
onnx.save(sm, '/tmp/matcha-icefall-zh-baker/matcha_C512.onnx')

# ---- full A+B+C verification ----
sO = ort.InferenceSession(SRC, providers=['CPUExecutionProvider'])
sA = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_A256.onnx',
                          providers=['CPUExecutionProvider'])
sC = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C512.onnx',
                          providers=['CPUExecutionProvider'])
print('C inputs:', [(i.name, i.shape) for i in sC.get_inputs()])

rng = np.random.default_rng(11)
tests = [('short', ids)]
tests.append(('long', np.asarray([1] + [v for t in rng.integers(4, 2000, 40) for v in (int(t), 1)], np.int64)))
ok_all = True
for tag, t_ids in tests:
    L = len(t_ids)
    x_pad = np.full((1, XB), 1, np.int64)
    x_pad[0, :L] = t_ids
    mel_o = sO.run(['mel'], {'x': t_ids[None, :], 'x_length': np.array([L], np.int64),
                             'noise_scale': np.array([0.0], np.float32),
                             'length_scale': np.array([1.0], np.float32)})[0]
    afeed = {'x': x_pad, 'x_length': np.array([L], np.int64),
             'length_scale': np.array([1.0], np.float32)}
    for i in sA.get_inputs():
        if i.name == 'noise_scale':
            afeed[i.name] = np.array([0.0], np.float32)
    dur, hid = sA.run(None, afeed)
    d = dur[0, :L]
    cum = np.cumsum(d)
    Lp = int(round(cum[-1]))
    oh = np.zeros((MB, XB), np.float32)
    for n_i in range(L):
        lo = 0 if n_i == 0 else int(round(cum[n_i - 1]))
        oh[lo:int(round(cum[n_i])), n_i] = 1.0
    mm = (oh @ hid[0].T)[None].astype(np.float32)
    dur3 = np.zeros((1, 1, XB), np.float32)
    dur3[0, 0, :L] = d
    mask = (np.arange(MB) < Lp).astype(np.float32)[None, None, :]
    mel_c = sC.run(None, {'/MatMul_output_0': mm, '/Mul_1_output_0': dur3,
                          '/Cast_3_output_0': mask,
                          'noise_scale': np.array([0.0], np.float32)})[0]
    k = min(Lp, mel_o.shape[2])
    diff = np.abs(mel_c[0, :, :k] - mel_o[0, :, :k])
    ok = diff.max() < 5e-3 and Lp == mel_o.shape[2]
    ok_all &= ok
    print(f'{tag}: L={L} Lp={Lp}/{mel_o.shape[2]} maxdiff={diff.max():.6f} '
          f'mean={diff.mean():.6f} -> {"OK" if ok else "FAIL"}')
print('ALL OK' if ok_all else 'SOME FAIL')
