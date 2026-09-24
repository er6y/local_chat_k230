# board: kv6 clean-data timing (5 inputs, valid random windows)
import sys, time
import numpy as np
import nncaseruntime as nn

W = 32
KM = sys.argv[1] if len(sys.argv) > 1 else '/mnt/data/static/llm_kv6_q2cpu.kmodel'
REPS = int(sys.argv[2]) if len(sys.argv) > 2 else 20
itp = nn.Interpreter()
itp.load_model(open(KM, 'rb').read())
rng = np.random.default_rng(9)
x = (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32)
m = np.zeros((1, 1, 1, W + 1), np.float32)
pos = np.array([[21]], np.int32)
kt_all = (rng.standard_normal((48, 64, W)) * 0.2).astype(np.float32)
v_all = (rng.standard_normal((48, W, 64)) * 0.2).astype(np.float32)
feeds = [x, m, pos, kt_all, v_all]
for i, t in enumerate(feeds):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
for _ in range(3):
    itp.run()
ts = []
for _ in range(REPS):
    t0 = time.time()
    itp.run()
    ts.append((time.time() - t0) * 1000)
ts = ts[2:]
print('%s clean-data: mean=%.1fms min=%.1fms max=%.1fms' % (KM, sum(ts) / len(ts), min(ts), max(ts)))
lg = itp.get_output_tensor(0).to_numpy()
print('argmax=%d' % int(lg.reshape(-1).argmax()))
print('KV6_TIME_DONE')
