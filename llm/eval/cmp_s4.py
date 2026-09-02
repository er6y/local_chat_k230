import numpy as np
y = np.load('/tmp/kpu_poc/qwen_s4/l0_gate.probe_y.npy').reshape(-1)
o = np.fromfile('/mnt/d/work/git_dev/k230_prj/k230_llm/.tools/s4_gate.out', dtype='<f4').reshape(-1)
print('y', y.shape, 'o', o.shape)
cs = float(np.dot(y, o) / (np.linalg.norm(y) * np.linalg.norm(o)))
rl = float(np.linalg.norm(y - o) / np.linalg.norm(y))
print('cos_sim %.6f rel_l2 %.4f' % (cs, rl))
