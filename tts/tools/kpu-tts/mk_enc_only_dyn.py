#!/usr/bin/env python3
# mk_enc_only_dyn.py — physically cut enc_dp_dynamic at the dp boundary:
# outputs = mu, logs, dp_x (Transpose). The duration_predictor subgraph is
# deleted, so no ORT output-pruning is needed on the board.
import onnx
import collections
import numpy as np
import onnxruntime as ort

SRC = '/tmp/vits-icefall-zh-aishell3/enc_dp_dynamic.onnx'
DST = '/tmp/vits-icefall-zh-aishell3/enc_only_dyn.onnx'
X = '/text_encoder/Transpose_output_0'

m = onnx.load(SRC)
G = m.graph
print('orig outputs:', [o.name for o in G.output])

# keep nodes reachable from the three enc outputs
producers = {}
for n in G.node:
    for o in n.output:
        producers[o] = n
wanted = ['/text_encoder/Split_output_0', '/text_encoder/Split_output_1', X]
needed = set()
stack = list(wanted)
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
print('nodes:', len(G.node), '->', len(keep))

del G.node[:]
del G.initializer[:]
del G.output[:]
G.initializer.extend(live_inits)
G.node.extend(keep)
G.output.extend([
    onnx.helper.make_tensor_value_info('/text_encoder/Split_output_0', onnx.TensorProto.FLOAT, [1, 96, 'T']),
    onnx.helper.make_tensor_value_info('/text_encoder/Split_output_1', onnx.TensorProto.FLOAT, [1, 96, 'T']),
    onnx.helper.make_tensor_value_info(X, onnx.TensorProto.FLOAT, [1, 96, 'T']),
])
onnx.save(m, DST)

c = collections.Counter(n.op_type for n in keep)
print('op mix:', dict(c))

# verify vs parent (mu/logs/dp_x) + timing
s_new = ort.InferenceSession(DST, providers=['CPUExecutionProvider'])
m2 = onnx.load(SRC)
m2.graph.output.append(onnx.helper.make_tensor_value_info(X, onnx.TensorProto.FLOAT, [1, 96, 'T']))
s_par = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
rng = np.random.default_rng(3)
for L in (17, 44):
    tok = rng.integers(1, 50, (1, L)).astype(np.int64)
    tl = np.array([L], np.int64)
    spk = np.array([10], np.int64)
    feed = {'tokens': tok, 'tokens_lens': tl, 'speaker': spk,
            'noise_scale': np.array([0.667], np.float32),
            'alpha': np.array([1.0], np.float32),
            'noise_scale_dur': np.array([0.8], np.float32)}
    o_new = s_new.run(None, feed)
    o_par = s_par.run(['/text_encoder/Split_output_0', '/text_encoder/Split_output_1', X], feed)
    eq = all(np.array_equal(a, b) for a, b in zip(o_new, o_par))
    print(f'L={L}: bit-exact vs parent = {eq}')
import time
tok = rng.integers(1, 50, (1, 44)).astype(np.int64)
feed = {'tokens': tok, 'tokens_lens': np.array([44], np.int64), 'speaker': np.array([10], np.int64),
        'noise_scale': np.array([0.667], np.float32), 'alpha': np.array([1.0], np.float32),
        'noise_scale_dur': np.array([0.8], np.float32)}
s_new.run(None, feed)
t0 = time.perf_counter()
for _ in range(10):
    s_new.run(None, feed)
print(f'x86 enc-only timing: {(time.perf_counter()-t0)*100:.1f} ms/sentence')
