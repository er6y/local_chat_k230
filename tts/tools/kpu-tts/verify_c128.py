#!/usr/bin/env python3
# verify_c128.py — C128 vs the ORIGINAL graph run at exactly L'=128
# (via length_scale tuning): the true windowed ground truth.
import numpy as np
import onnx
import onnxruntime as ort

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
WB, XB = 128, 256

m2 = onnx.load(SRC)
for w in ['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0']:
    m2.graph.output.append(onnx.helper.make_tensor_value_info(w, onnx.TensorProto.FLOAT, None))
s2 = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
sC = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C128.onnx',
                          providers=['CPUExecutionProvider'])

rng = np.random.default_rng(3)
tok = rng.integers(4, 2000, 10).astype(np.int64)
ids = np.asarray([1] + [v for t in tok for v in (int(t), 1)], np.int64)

# pass 1: ls=1 to get Lp0
def run_orig(ls):
    return s2.run(['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0'],
                  {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
                   'noise_scale': np.array([0.0], np.float32),
                   'length_scale': np.array([ls], np.float32)})

mm1, c31, dur1 = run_orig(1.0)
Lp0 = mm1.shape[1]
ls = 128.0 / Lp0
mm_o, c3_o, dur_o = run_orig(ls)
Lp = mm_o.shape[1]
print(f'Lp0={Lp0} ls={ls:.4f} -> Lp={Lp}')
assert Lp == 128, Lp

mel_o = ort.InferenceSession(SRC, providers=['CPUExecutionProvider']).run(
    ['mel'], {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
              'noise_scale': np.array([0.0], np.float32),
              'length_scale': np.array([ls], np.float32)})[0]
print('orig mel at exact 128:', mel_o.shape)

Db = np.zeros((1, 1, XB), np.float32)
Db[0, 0, :dur_o.shape[2]] = dur_o[0]
mel_c = sC.run(None, {'/MatMul_output_0': mm_o, '/Cast_3_output_0': c3_o,
                      '/Mul_1_output_0': Db,
                      'noise_scale': np.array([0.0], np.float32)})[0]
d = np.abs(mel_c - mel_o)
print(f'C128 vs original@exact128: maxdiff={d.max():.6f} mean={d.mean():.6f} '
      f'-> {"OK" if d.max() < 5e-3 else "FAIL"}')
