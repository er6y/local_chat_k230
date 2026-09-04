#!/usr/bin/env python3
# mk_dp_only.py (v2) — cut the canvas graph at the REAL enc/dp boundary:
#   /text_encoder/Transpose_output_0  (after_norm, perm [1,2,0]) -> [1,96,64]
#   consumed by /duration_predictor/pre/Conv
# The enc (int8-hostile transformer) is dropped entirely; the dp-only graph
# takes dp_x (f32) + tokens/lens/speaker/nsd/alpha. All enc-side quantization
# error is excluded by construction.
import onnx
import collections

SRC = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_pruned.onnx'
DST = '/tmp/vits-icefall-zh-aishell3/dp_only.onnx'
X = '/text_encoder/Transpose_output_0'
X_IN = 'dp_x'

m = onnx.load(SRC)
G = m.graph

n_re = 0
for n in G.node:
    for j, i in enumerate(n.input):
        if i == X:
            n.input[j] = X_IN
            n_re += 1
print('rewired', n_re, 'dp_x consumers')
assert n_re == 1, f'expected exactly 1 consumer, got {n_re}'

del G.input[:]
G.input.extend([
    onnx.helper.make_tensor_value_info(X_IN, onnx.TensorProto.FLOAT, [1, 96, 64]),
    onnx.helper.make_tensor_value_info('tokens', onnx.TensorProto.INT64, [1, 64]),
    onnx.helper.make_tensor_value_info('tokens_lens', onnx.TensorProto.INT64, [1]),
    onnx.helper.make_tensor_value_info('speaker', onnx.TensorProto.INT64, [1]),
    onnx.helper.make_tensor_value_info('noise_scale_dur', onnx.TensorProto.FLOAT, [1]),
    onnx.helper.make_tensor_value_info('alpha', onnx.TensorProto.FLOAT, [1]),
])

producers = {}
for n in G.node:
    for o in n.output:
        producers[o] = n
needed = set()
stack = [o.name for o in G.output]
while stack:
    t = stack.pop()
    if t in needed:
        continue
    needed.add(t)
    n = producers.get(t)
    if n is not None:
        stack.extend(n.input)
keep = [n for n in G.node if any(o in needed for o in n.output)]
live_inits = [i for i in G.initializer if i.name in needed]
print('nodes:', len(G.node), '->', len(keep), ' inits:', len(G.initializer), '->', len(live_inits))

produced = set(i.name for i in live_inits)
produced.update(i.name for i in G.input)
remaining = list(keep)
sorted_nodes = []
while remaining:
    progressed = False
    for n in list(remaining):
        if all(i in produced or i == '' for i in n.input):
            sorted_nodes.append(n)
            remaining.remove(n)
            produced.update(n.output)
            progressed = True
            break
    if not progressed:
        raise RuntimeError('unsatisfiable: ' + remaining[0].name)
del G.node[:]
G.node.extend(sorted_nodes)
del G.initializer[:]
G.initializer.extend(live_inits)
onnx.save(m, DST)

c = collections.Counter(n.op_type for n in sorted_nodes)
print('op mix:', dict(c))
print('saved', DST)

# ---- ORT A/B: dp_only(dp_x) must equal canvas(tokens) bit-exact at nsd=0 ----
import numpy as np
import onnxruntime as ort

m2 = onnx.load(SRC)
m2.graph.output.append(onnx.helper.make_tensor_value_info(X, onnx.TensorProto.FLOAT, [1, 96, 64]))
s_full = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
s_only = ort.InferenceSession(DST, providers=['CPUExecutionProvider'])

rng = np.random.default_rng(5)
for k in range(3):
    tok = rng.integers(1, 50, (1, 64)).astype(np.int64)
    if k == 2:
        tok = np.zeros((1, 64), np.int64)
        tok[0, :15] = rng.integers(1, 50, 15)
    tl = np.array([int((tok[0] != 0).sum())], np.int64)
    spk = np.array([10 + k], np.int64)
    nsd = np.array([0.0], np.float32)
    al = np.array([1.0], np.float32)
    feed_full = {'tokens': tok, 'tokens_lens': tl, 'speaker': spk,
                 'noise_scale_dur': nsd, 'alpha': al}
    ref, dp_x = s_full.run(['/Mul_1_output_0', X], feed_full)
    out = s_only.run(None, {'dp_x': dp_x, 'tokens': tok, 'tokens_lens': tl,
                            'speaker': spk, 'noise_scale_dur': nsd, 'alpha': al})[0]
    eq = np.array_equal(ref, out)
    print(f'case {k}: bit-exact={eq} maxdiff={np.abs(ref - out).max():.3e}')
    assert eq
print('DP-ONLY A/B PASS')
