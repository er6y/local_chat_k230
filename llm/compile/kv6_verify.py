#!/usr/bin/env python3
# ORT equivalence: kvwin32 (96 window inputs) vs kv6 (2 stacked inputs)
import onnxruntime as ort
import numpy as np

SRC = '/root/k230/input/qwen25_24l_s1h256_kvwin32.onnx'
DST = '/root/k230/input/qwen25_24l_s1h256_kv6.onnx'
W = 32
rng = np.random.default_rng(11)

so = ort.SessionOptions()
so.graph_optimization_level = ort.GraphOptimizationLevel.ORT_DISABLE_ALL
s1 = ort.InferenceSession(SRC, so, providers=['CPUExecutionProvider'])
s2 = ort.InferenceSession(DST, so, providers=['CPUExecutionProvider'])

H = 896
x = (rng.standard_normal((1, 1, H)) * 0.3).astype(np.float32)
mask = np.zeros((1, 1, 1, W + 1), np.float32)
pos = np.array([[21]], np.int32)
ktw = (rng.standard_normal((24, 2, 64, W)) * 0.2).astype(np.float32)
vtw = (rng.standard_normal((24, 2, W, 64)) * 0.2).astype(np.float32)

f1 = {'input_ids': x, 'attention_mask': mask, 'position_ids': pos}
for l in range(24):
    for g in range(2):
        f1['ktwin_%02d_%d' % (l, g)] = ktw[l, g]
        f1['vwin_%02d_%d' % (l, g)] = vtw[l, g]
o1 = s1.run(None, f1)

f2 = {'input_ids': x, 'attention_mask': mask, 'position_ids': pos,
      'kt_all': ktw.reshape(48, 64, W), 'v_all': vtw.reshape(48, W, 64)}
o2 = s2.run(None, f2)

d = np.abs(o1[0] - o2[0]).max()
am1, am2 = int(o1[0].argmax()), int(o2[0].argmax())
kv1 = np.stack([np.concatenate([o1[1 + l * 4 + g].reshape(-1) for g in range(2)]) for l in range(24)])
kv2 = np.stack([np.concatenate([o2[1 + l * 4 + g].reshape(-1) for g in range(2)]) for l in range(24)])
kv2v = np.stack([np.concatenate([o2[1 + l * 4 + 2 + g].reshape(-1) for g in range(2)]) for l in range(24)])
kv1v = np.stack([np.concatenate([o1[1 + l * 4 + 2 + g].reshape(-1) for g in range(2)]) for l in range(24)])
print('logits max_diff=%.3e argmax %d vs %d' % (d, am1, am2))
print('ktnew max_diff=%.3e  vtnew max_diff=%.3e' % (np.abs(kv1 - kv2).max(), np.abs(kv1v - kv2v).max()))
ok = d < 1e-4 and np.abs(kv1 - kv2).max() < 1e-4 and am1 == am2
print('KV6_VERIFY', 'PASS' if ok else 'FAIL')
