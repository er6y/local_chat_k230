#!/usr/bin/env python3
# mk_enc_sim.py — onnxsim the enc-only dynamic graph WITHOUT pinning shapes
# (dynamic_input_shape mode), verify bit-exact at several lengths.
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/vits-icefall-zh-aishell3/enc_only_dyn.onnx'
DST = '/tmp/vits-icefall-zh-aishell3/enc_only_sim.onnx'

m = onnx.load(SRC)
sm, ok = simplify(m, dynamic_input_shape=True, skip_fuse_bn=True,
                  skipped_optimizers=['fuse_consecutive_transposes',
                                      'eliminate_duplicate_initializer'])
print('simplify ok:', ok, 'nodes:', len(m.graph.node), '->', len(sm.graph.node))
onnx.save(sm, DST)
print('inputs:', [(i.name, [d.dim_value if d.HasField('dim_value') else d.dim_param
                             for d in i.type.tensor_type.shape.dim]) for i in sm.graph.input])
print('outputs:', [(o.name, [d.dim_value if d.HasField('dim_value') else d.dim_param
                              for d in o.type.tensor_type.shape.dim]) for o in sm.graph.output])

s0 = ort.InferenceSession(SRC, providers=['CPUExecutionProvider'])
s1 = ort.InferenceSession(DST, providers=['CPUExecutionProvider'])
rng = np.random.default_rng(9)
allok = True
for L in (3, 17, 25, 44, 60):
    tok = rng.integers(1, 50, (1, L)).astype(np.int64)
    tl = np.array([L], np.int64)
    spk = np.array([10], np.int64)
    feed = {'tokens': tok, 'tokens_lens': tl, 'speaker': spk,
            'noise_scale': np.array([0.667], np.float32),
            'alpha': np.array([1.0], np.float32),
            'noise_scale_dur': np.array([0.8], np.float32)}
    a = s0.run(None, feed)
    b = s1.run(None, feed)
    eq = all(np.allclose(x, y, rtol=0, atol=0) for x, y in zip(a, b))
    allok &= eq
    print(f'L={L}: bit-exact={eq} shapes={[x.shape for x in b]}')
print('PASS' if allok else 'FAIL')
