# board: profile kv6 with valid feeds (5 inputs)
import sys, time
import numpy as np
import nncaseruntime as nn

W = 32
KM = '/mnt/data/static/llm_kv6_q2cpu.kmodel'
itp = nn.Interpreter()
itp.load_model(open(KM, 'rb').read())

rng = np.random.default_rng(9)
x = (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32)
m = np.zeros((1, 1, 1, W + 1), np.float32)
pos = np.array([[21]], np.int32)
kt_all = (rng.standard_normal((48, 64, W)) * 0.2).astype(np.float32)
v_all = (rng.standard_normal((48, W, 64)) * 0.2).astype(np.float32)

itp.set_profiling(True)
for i, t in enumerate([x, m, pos, kt_all, v_all]):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))

r = itp.run()
out = itp.get_output_tensor(0).to_numpy()
print('logits argmax=%d' % int(out.reshape(-1).argmax()), flush=True)
for k in range(3):
    t0 = time.time()
    itp.run()
    print('run%d %.3fs' % (k, time.time() - t0), flush=True)
print('PROFKV6_DONE', flush=True)
