# board: profile a s3c variant kmodel (feed by variant-specific order)
import sys
import time
import numpy as np
import nncaseruntime as nn

KM = sys.argv[1]
TAG = sys.argv[2]  # nopos | nokv | full
rng = np.random.default_rng(9)
feeds_all = {
    'input_ids': (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32),
    'attention_mask': np.zeros((1, 1, 1, 33), np.float32),
    'position_ids': np.array([[21]], np.int32),
    'kt_all': (rng.standard_normal((48, 64, 32)) * 0.2).astype(np.float32),
    'v_all': (rng.standard_normal((48, 32, 64)) * 0.2).astype(np.float32),
}
ORDERS = {
    'full': ['input_ids', 'attention_mask', 'position_ids', 'kt_all', 'v_all'],
    'nopos': ['input_ids', 'attention_mask', 'kt_all', 'v_all'],
    'nokv': ['input_ids', 'attention_mask', 'position_ids'],
    'nomask': ['input_ids', 'position_ids', 'kt_all', 'v_all'],
    'nox': ['attention_mask', 'position_ids', 'kt_all', 'v_all'],
}
order = ORDERS[TAG]
itp = nn.Interpreter()
itp.load_model(open(KM, 'rb').read())
itp.set_profiling(True)
for i, name in enumerate(order):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(feeds_all[name])))
out = itp.run()
for k in range(2):
    t0 = time.time()
    itp.run()
    print('run%d %.4fs' % (k, time.time() - t0), flush=True)
print('PROFVAR_DONE', flush=True)
