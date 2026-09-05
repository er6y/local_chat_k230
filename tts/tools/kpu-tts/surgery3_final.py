#!/usr/bin/env python3
# surgery3_final.py — final 3-way split with pinned buckets + full verification.
#   A: x[1,256] -> durations [1,256], hidden [1,80,256]   (pads beyond L)
#   B: numpy one-hot LR (runner/CPU)
#   C: mm_out[1,512,80] + ns -> mel[1,80,512]  (runner slices to L')
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
DUR = '/Squeeze_output_0'
HID = '/encoder/Mul_1_output_0'
XB = 256   # token bucket
MB = 512   # mel-frame bucket

m = onnx.load(SRC)
G = m.graph
prod = {}
for n in G.node:
    for o in n.output:
        prod[o] = n
matmul = next(n for n in G.node if n.op_type == 'MatMul' and '/Transpose_1_output_0' in n.input)
s2 = next(n for n in G.node if n.name == '/Slice_2')     # dynamic final slice
C_OUT = s2.input[0]                                        # pre-slice full mel


def build(src_path, targets, stop_names, out_names, pin=None, dst=None):
    mm = onnx.load(src_path)
    gg = mm.graph
    pr = {}
    for n in gg.node:
        for o in n.output:
            pr[o] = n
    need = set()
    stack = list(targets)
    while stack:
        t = stack.pop()
        if t in need:
            continue
        need.add(t)
        n = pr.get(t)
        if n is not None and n.name not in stop_names:
            stack.extend(n.input)
    keep = [n for n in gg.node if n.name not in stop_names and any(o in need for o in n.output)]
    inits = [i for i in gg.initializer if i.name in need]
    del gg.node[:]
    del gg.initializer[:]
    del gg.output[:]
    gg.node.extend(keep)
    gg.initializer.extend(inits)
    for name, dt, shape in out_names:
        gg.output.append(onnx.helper.make_tensor_value_info(name, dt, shape))
    # inputs: original graph inputs actually consumed + dangling cut tensors
    used = {i for n in keep for i in n.input}
    internal = {o for n in keep for o in n.output}
    del gg.input[:]
    for i in onnx.load(src_path).graph.input:
        if i.name in used:
            gg.input.append(i)
    for u in sorted(used):
        if u and u not in internal and u not in {i.name for i in gg.input}            and not any(i.name == u for i in gg.initializer):
            gg.input.append(onnx.helper.make_tensor_value_info(
                u, onnx.TensorProto.FLOAT, None))
    # topo
    produced = set(i.name for i in gg.initializer)
    produced.update(i.name for i in gg.input)
    rem = list(gg.node)
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
    del gg.node[:]
    gg.node.extend(srt)
    if pin:
        pin = {k: v for k, v in pin.items()
               if any(i.name == k for i in mm.graph.input)}
        print('  graph inputs:', [i.name for i in mm.graph.input])
        sm, ok = simplify(mm, overwrite_input_shapes=pin, skip_fuse_bn=True)
        print(f'  sim ok={ok} nodes {len(srt)} -> {len(sm.graph.node)}')
        mm = sm
    onnx.save(mm, dst)
    return mm


print('== Part A ==')
build(SRC, [DUR, HID], set(),
      [(DUR, onnx.TensorProto.FLOAT, [1, XB]), (HID, onnx.TensorProto.FLOAT, [1, 80, XB])],
      pin={'x': [1, XB], 'x_length': [1], 'noise_scale': [1], 'length_scale': [1]},
      dst='/tmp/matcha-icefall-zh-baker/matcha_A256.onnx')

print('== Part C ==')
build(SRC, [C_OUT], {matmul.name},
      [(C_OUT, onnx.TensorProto.FLOAT, [1, 80, MB])],
      pin={matmul.output[0]: [1, MB, 80], 'noise_scale': [1]},
      dst='/tmp/matcha-icefall-zh-baker/matcha_C512.onnx')

# ---------------- verification ----------------
sO = ort.InferenceSession(SRC, providers=['CPUExecutionProvider'])
sA = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_A256.onnx',
                          providers=['CPUExecutionProvider'])
sC = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C512.onnx',
                          providers=['CPUExecutionProvider'])
print('A inputs:', [(i.name, i.shape) for i in sA.get_inputs()])
print('A outputs:', [(o.name, o.shape) for o in sA.get_outputs()])
print('C inputs:', [(i.name, i.shape) for i in sC.get_inputs()])
print('C outputs:', [(o.name, o.shape) for o in sC.get_outputs()])

tok = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
ids = [1] + [v for t in tok for v in (t, 1)]
L = len(ids)
pad = 1  # pad_id
x = np.full((1, XB), pad, np.int64)
x[0, :L] = ids
mel_orig = sO.run(['mel'], {'x': np.asarray(ids, np.int64)[None, :],
                            'x_length': np.array([L], np.int64),
                            'noise_scale': np.array([0.0], np.float32),
                            'length_scale': np.array([1.0], np.float32)})[0]

afeed = {'x': x, 'x_length': np.array([L], np.int64)}
for i in sA.get_inputs():
    if i.name == 'noise_scale':
        afeed[i.name] = np.array([0.0], np.float32)
    elif i.name == 'length_scale':
        afeed[i.name] = np.array([1.0], np.float32)
dur, hid = sA.run(None, afeed)
print('A out: dur', dur.shape, 'hid', hid.shape, 'Lp=', float(dur[0, :L].sum()))

# Part B
cum = np.cumsum(dur[0, :L])
Lp = int(round(cum[-1]))
oh = np.zeros((MB, XB), np.float32)
for n_i in range(L):
    lo = 0 if n_i == 0 else int(round(cum[n_i - 1]))
    hi = int(round(cum[n_i]))
    oh[lo:hi, n_i] = 1.0
h_up = oh @ hid[0].T  # [MB, 80]
print('B: Lp =', Lp)

cfeed = {}
for i in sC.get_inputs():
    if i.name == matmul.output[0]:
        cfeed[i.name] = h_up[None, :, :]
    elif i.name == 'noise_scale':
        cfeed[i.name] = np.array([0.0], np.float32)
    else:
        raise RuntimeError('unexpected C input: ' + i.name)
mel_c = sC.run(None, cfeed)[0]
print('C out:', mel_c.shape, 'orig:', mel_orig.shape)
k = min(mel_c.shape[2], mel_orig.shape[2])
d = np.abs(mel_c[0, :, :k] - mel_orig[0, :, :k])
print(f'3-way vs original (ns=0): maxdiff={d.max():.6f} mean={d.mean():.6f}')
