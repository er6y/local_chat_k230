# board: profile s3c layer-0 slice (5 inputs like kv6)
import time
import numpy as np
import nncaseruntime as nn

KM = '/mnt/data/kmini/s3c.kmodel'
itp = nn.Interpreter()
itp.load_model(open(KM, 'rb').read())
rng = np.random.default_rng(9)
x = (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32)
m = np.zeros((1, 1, 1, 33), np.float32)
pos = np.array([[21]], np.int32)
kt = (rng.standard_normal((48, 64, 32)) * 0.2).astype(np.float32)
v = (rng.standard_normal((48, 32, 64)) * 0.2).astype(np.float32)
itp.set_profiling(True)
for i, t in enumerate([x, m, pos, kt, v]):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
out = itp.run()
for k in range(3):
    t0 = time.time()
    itp.run()
    print('run%d %.4fs' % (k, time.time() - t0), flush=True)
print('PROFS3C_DONE', flush=True)
