#!/usr/bin/env python3
# mk_hifigan_static.py — pin hifigan_v2 mel input to [1,80,512], onnxsim,
# verify against the dynamic model (prefix comparison, receptive-field aware).
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/hifigan_v2.onnx'
DST = '/tmp/matcha-icefall-zh-baker/hifigan_b512.onnx'
BUCKET = 512

m = onnx.load(SRC)
# pin the mel input shape
for i in m.graph.input:
    if i.name == 'mel':
        t = i.type.tensor_type
        t.shape.Clear()
        for d in (1, 80, BUCKET):
            t.shape.dim.add().dim_value = d
sm, ok = simplify(m, overwrite_input_shapes={'mel': [1, 80, BUCKET]},
                  skip_fuse_bn=True)
print('simplify ok:', ok, 'nodes:', len(m.graph.node), '->', len(sm.graph.node))
onnx.save(sm, DST)

sd = ort.InferenceSession(SRC, providers=['CPUExecutionProvider'])
ss = ort.InferenceSession(DST, providers=['CPUExecutionProvider'])
print('static io:', [(i.name, i.shape) for i in ss.get_inputs()],
      [(o.name, o.shape) for o in ss.get_outputs()])

rng = np.random.default_rng(0)
for L in (200, 304, 500):
    mel = rng.normal(0, 1, (1, 80, L)).astype(np.float32) * 0.5
    w_dyn = sd.run(None, {'mel': mel})[0]
    mel_pad = np.zeros((1, 80, BUCKET), np.float32)
    mel_pad[:, :, :L] = mel
    w_sta = ss.run(None, {'mel': mel_pad})[0]
    n = L * 256
    a = w_dyn.flatten()[:n].astype(np.float64)
    b = w_sta.flatten()[:n].astype(np.float64)
    d = np.abs(a - b)
    # divergence onset: last index where |d| stays below 1e-3
    ok_prefix = int(np.argmax(d > 1e-3)) if (d > 1e-3).any() else n
    cos = float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-30))
    print(f'L={L}: prefix_clean={ok_prefix}/{n} cos={cos:.6f} maxdiff={d.max():.4f}')
