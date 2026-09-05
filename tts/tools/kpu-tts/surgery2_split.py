#!/usr/bin/env python3
# surgery2_split.py — build Part A (encoder+DP) and Part C (decoder+ODE) graphs,
# implement Part B (one-hot LR) in numpy, verify A+B+C vs the original acoustic.
import numpy as np
import onnx
import onnxruntime as ort
import collections

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
PA = '/tmp/matcha-icefall-zh-baker/matcha_A.onnx'
PC = '/tmp/matcha-icefall-zh-baker/matcha_C.onnx'

m = onnx.load(SRC)
G = m.graph
prod = {}
for n in G.node:
    for o in n.output:
        prod[o] = n

DUR = '/Squeeze_output_0'           # [1, N] f32 durations
HID = '/encoder/Mul_1_output_0'     # [1, 80, N] f32 hidden
matmul = next(n for n in G.node if n.op_type == 'MatMul' and '/Transpose_1_output_0' in n.input)
MM_OUT = matmul.output[0]           # [1, L', 80]

# ---- Part A: keep nodes reaching DUR or HID from the graph inputs ----
targets_A = [DUR, HID]
needed = set()
stack = list(targets_A)
while stack:
    t = stack.pop()
    if t in needed:
        continue
    needed.add(t)
    n = prod.get(t)
    if n is not None:
        stack.extend(n.input)
mA = onnx.load(SRC)
keepA = [n for n in mA.graph.node if any(o in needed for o in n.output)]
initA = [i for i in mA.graph.initializer if i.name in needed]
del mA.graph.node[:]
del mA.graph.initializer[:]
del mA.graph.output[:]
mA.graph.node.extend(keepA)
mA.graph.initializer.extend(initA)
mA.graph.output.extend([
    onnx.helper.make_tensor_value_info(DUR, onnx.TensorProto.FLOAT, ['N', 'T']),
    onnx.helper.make_tensor_value_info(HID, onnx.TensorProto.FLOAT, ['N', 80, 'T']),
])
# topo sort
produced = set(i.name for i in initA)
produced.update(i.name for i in mA.graph.input)
rem = list(mA.graph.node)
srt = []
while rem:
    for n in list(rem):
        if all(i in produced or i == '' for i in n.input):
            srt.append(n); rem.remove(n); produced.update(n.output)
            break
    else:
        raise RuntimeError('A unsat: ' + rem[0].name)
del mA.graph.node[:]
mA.graph.node.extend(srt)
onnx.save(mA, PA)
print('Part A:', len(srt), 'nodes')

# ---- Part C: keep nodes reaching 'mel' from MM_OUT + extra inputs ----
# C's external inputs: the MatMul's operands chain start. We cut C's entry at
# MM_OUT: C input = the MatMul output? Cleaner: feed C with the upsampled
# hidden directly — i.e. C starts AFTER the MatMul (B does the matmul).
mC = onnx.load(SRC)
prodC = {}
for n in mC.graph.node:
    for o in n.output:
        prodC[o] = n
neededC = set()
stack = ['mel']
while stack:
    t = stack.pop()
    if t in neededC:
        continue
    neededC.add(t)
    n = prodC.get(t)
    if n is not None:
        if n.name == matmul.name:
            continue  # cut boundary: the one-hot MatMul is Part B's job
        stack.extend(n.input)
keepC = [n for n in mC.graph.node if n.name != matmul.name and any(o in neededC for o in n.output)]
# external tensors consumed by kept nodes that are NOT produced within C
internal = set()
for n in keepC:
    internal.update(n.output)
ext = set()
for n in keepC:
    for i in n.input:
        if i not in internal and i not in set(x.name for x in mC.graph.initializer) \
           and i not in set(x.name for x in mC.graph.input) and i != '':
            ext.add(i)
print('C external inputs:', ext)
initC = [i for i in mC.graph.initializer if i.name in neededC]
del mC.graph.node[:]
del mC.graph.initializer[:]
del mC.graph.output[:]
mC.graph.node.extend(keepC)
mC.graph.initializer.extend(initC)
# graph inputs: the external tensors + original scalar inputs it still uses
used_scalars = [i.name for i in mC.graph.input if i.name in
                {x for n in keepC for x in n.input}]
del mC.graph.input[:]
for e in sorted(ext):
    mC.graph.input.append(onnx.helper.make_tensor_value_info(e, onnx.TensorProto.FLOAT, None))
for s in used_scalars:
    if s not in ext:
        et = onnx.TensorProto.FLOAT if s == 'noise_scale' else onnx.TensorProto.INT64
        mC.graph.input.append(onnx.helper.make_tensor_value_info(s, et, None))
mC.graph.output.append(onnx.helper.make_tensor_value_info(
    'mel', onnx.TensorProto.FLOAT, ['N', 80, 'L']))
produced = set(i.name for i in mC.graph.initializer)
produced.update(i.name for i in mC.graph.input)
rem = list(mC.graph.node)
srt = []
while rem:
    for n in list(rem):
        if all(i in produced or i == '' for i in n.input):
            srt.append(n); rem.remove(n); produced.update(n.output)
            break
    else:
        raise RuntimeError('C unsat: ' + rem[0].name)
del mC.graph.node[:]
mC.graph.node.extend(srt)
onnx.save(mC, PC)
print('Part C:', len(srt), 'nodes, inputs:', [i.name for i in mC.graph.input])

# ---- verification: A -> B(numpy) -> C vs original (ns=0) ----
sA = ort.InferenceSession(PA, providers=['CPUExecutionProvider'])
sC = ort.InferenceSession(PC, providers=['CPUExecutionProvider'])
sO = ort.InferenceSession(SRC, providers=['CPUExecutionProvider'])

tok = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
ids = [1] + [v for t in tok for v in (t, 1)]
x = np.asarray(ids, np.int64)[None, :]
L = x.shape[1]
feed = {'x': x, 'x_length': np.array([L], np.int64),
        'noise_scale': np.array([0.0], np.float32),
        'length_scale': np.array([1.0], np.float32)}
mel_orig = sO.run(['mel'], feed)[0]

dur, hid = sA.run(None, {'x': x, 'x_length': np.array([L], np.int64),
                         'noise_scale': np.array([0.0], np.float32),
                         'length_scale': np.array([1.0], np.float32)})
print('A: dur', dur.shape, 'hid', hid.shape, 'Lp =', float(dur.sum()))

# Part B: one-hot [1, L', N] = frame t belongs to token n
cum = np.cumsum(dur[0])
Lp = int(round(cum[-1]))
t = np.arange(Lp, dtype=np.int64)
oh = np.zeros((Lp, dur.shape[1]), np.float32)
for n_i in range(dur.shape[1]):
    lo = 0 if n_i == 0 else int(round(cum[n_i - 1]))
    hi = int(round(cum[n_i]))
    oh[lo:hi, n_i] = 1.0
h_up = oh @ hid[0].T  # [L', 80]

# Part C: input names?
cins = [i.name for i in sC.get_inputs()]
print('C runtime inputs:', cins)
cfeed = {}
for name in cins:
    if name == MM_OUT:
        cfeed[name] = h_up[None, :, :]
    elif name == 'noise_scale':
        cfeed[name] = np.array([0.0], np.float32)
    elif name == 'length_scale':
        cfeed[name] = np.array([1.0], np.float32)
    else:
        raise RuntimeError('unexpected C input ' + name)
mel_c = sC.run(['mel'], cfeed)[0]
print('C: mel', mel_c.shape, 'orig mel', mel_orig.shape)
k = min(mel_c.shape[2], mel_orig.shape[2])
d = np.abs(mel_c[0, :, :k] - mel_orig[0, :, :k])
print(f'A+B+C vs original (ns=0): maxdiff={d.max():.6f} mean={d.mean():.6f} '
      f'frames k={k}')
