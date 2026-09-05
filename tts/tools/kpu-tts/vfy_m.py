import numpy as np, onnxruntime as ort
rng = np.random.default_rng(0)
mm = (rng.normal(0, 1, (1, 128, 80)) * 0.3).astype(np.float32)
raw = rng.normal(0, 1, (1, 80, 128)).astype(np.float32)
mk = np.ones((1, 1, 128), np.float32)
ns_v = 0.667
sn = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C128n.onnx', providers=['CPUExecutionProvider'])
sm = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C128m.onnx', providers=['CPUExecutionProvider'])
du = np.full((1, 1, 256), 8.0, np.float32); du[0, 0, 200:] = 0.0
a = sn.run(None, {'noise': raw, '/MatMul_output_0': mm, '/Cast_3_output_0': mk,
                  '/Mul_1_output_0': du,
                  'noise_scale': np.array([ns_v], np.float32)})[0]
b = sm.run(None, {'noise': (raw * ns_v).astype(np.float32), '/MatMul_output_0': mm,
                  '/Cast_3_output_0': mk})[0]
print('C128m vs C128n matched-noise maxdiff:', float(np.abs(a - b).max()))
