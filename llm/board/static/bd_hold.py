# board: load model, run once, then HOLD so /proc can be probed
import sys
import time
import numpy as np
import nncaseruntime as nn

W = 32
KM = sys.argv[1] if len(sys.argv) > 1 else '/mnt/data/static/llm_kv6_stacked.kmodel'
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
itp.run()
print('HOLDING pid=%d' % np.inf if False else 'HOLDING', flush=True)
import os
print('PID=%d' % os.getpid(), flush=True)
time.sleep(180)
print('HOLD_DONE', flush=True)
