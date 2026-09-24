# profile OFFICIAL qwen kmodel: decode feeds per qwen_chat llm.cpp
# mask = [1,1,1,1] float 0.0 at seq_len==1 (kv_seq_len forced to seq_len!)
import sys, time
import numpy as np
import nncaseruntime as nn

KM = '/mnt/data/qwen_official/Qwen2.5-0.5B-Instruct/llm.kmodel'
itp = nn.Interpreter()
itp.load_model(open(KM, 'rb').read())
rng = np.random.default_rng(7)

H = 255
x = (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32)
m = np.zeros((1, 1, 1, 1), np.float32)
pos = np.array([[H]], np.int32)
past = (rng.standard_normal((24, 2, 1, H, 2, 64)) * 0.2).astype(np.float32)

itp.set_profiling(True)
for i, t in enumerate([x, m, pos, past]):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
out = itp.run()
o0 = itp.get_output_tensor(0).to_numpy()
print('out0 shape', o0.shape, 'argmax', int(o0.reshape(-1).argmax()), flush=True)
for k in range(3):
    t0 = time.time()
    itp.run()
    print('run%d %.3fs' % (k, time.time() - t0), flush=True)
print('PROFOFF_DONE', flush=True)
