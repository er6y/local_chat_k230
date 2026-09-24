# probe official llm.kmodel: real input descriptors + corrected-shape decode bench
import sys, time
import numpy as np
import nncaseruntime as nn

KM = '/mnt/data/qwen_official/Qwen2.5-0.5B-Instruct/llm.kmodel'
itp = nn.Interpreter()
itp.load_model(open(KM, 'rb').read())
n_in = itp.inputs_size
print('inputs:', n_in, flush=True)
desc = []
for i in range(n_in):
    t = itp.get_input_tensor(i)
    print(' in%d shape=%s dtype=%s' % (i, list(t.shape), t.dtype), flush=True)
    desc.append((list(t.shape), t.dtype))
n_out = itp.outputs_size
print('outputs:', n_out, flush=True)
for i in range(n_out):
    t = itp.get_output_tensor(i)
    print(' out%d shape=%s dtype=%s' % (i, list(t.shape), t.dtype), flush=True)

rng = np.random.default_rng(7)
feeds = []
for shape, dt in desc:
    shape = [int(d) if d > 0 else 1 for d in shape]
    if dt in (np.dtype('int32'), np.dtype('int64')):
        arr = np.zeros(shape, dt)
    else:
        arr = (rng.standard_normal(shape) * 0.3).astype(np.float32)
    feeds.append(arr)
for i, t in enumerate(feeds):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
itp.run(); itp.run()
ts = []
for _ in range(10):
    t0 = time.time(); itp.run(); ts.append((time.time() - t0) * 1000)
ts = ts[2:]
print('CORRECTED decode: mean=%.1fms min=%.1fms' % (sum(ts) / len(ts), min(ts)), flush=True)
o0 = itp.get_output_tensor(0).to_numpy()
print('argmax=%d' % int(o0.reshape(-1).argmax()), flush=True)
print('PROBE_DONE', flush=True)
