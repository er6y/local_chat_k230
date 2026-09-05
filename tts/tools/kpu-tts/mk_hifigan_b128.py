#!/usr/bin/env python3
# mk_hifigan_b128.py — small-bucket static hifigan (mel[1,80,128]) to dodge
# the tiled ConvTranspose path (b512 tiles the stride-2 CT into 3 pieces).
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/hifigan_v2.onnx'
DST = '/tmp/matcha-icefall-zh-baker/hifigan_b128.onnx'
BUCKET = 128

m = onnx.load(SRC)
for i in m.graph.input:
    if i.name == 'mel':
        t = i.type.tensor_type
        t.shape.Clear()
        for d in (1, 80, BUCKET):
            t.shape.dim.add().dim_value = d
sm, ok = simplify(m, overwrite_input_shapes={'mel': [1, 80, BUCKET]}, skip_fuse_bn=True)
print('simplify ok:', ok, 'nodes:', len(m.graph.node), '->', len(sm.graph.node))
onnx.save(sm, DST)

sd = ort.InferenceSession(SRC, providers=['CPUExecutionProvider'])
ss = ort.InferenceSession(DST, providers=['CPUExecutionProvider'])
rng = np.random.default_rng(0)
mel = rng.normal(0, 1, (1, 80, BUCKET)).astype(np.float32) * 0.5
a = sd.run(None, {'mel': mel})[0]
b = ss.run(None, {'mel': mel})[0]
print('full128 bit-exact:', np.array_equal(a, b), 'out', b.shape)
# save a 128-frame real-mel probe bin
cm = np.load('/tmp/kpu_poc/matcha_calib_full/mel_000.npy')[:, :, :BUCKET]
cm.flatten().tofile('/tmp/kpu_poc/probe_mel128.bin')
print('probe_mel128 saved, range', float(cm.min()), float(cm.max()))
