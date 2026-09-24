import sys
import numpy as np
import nncase

def run(km_path, feeds):
    sim = nncase.Simulator()
    sim.load_model(open(km_path, chr(114)+chr(98)).read())
    for i, t in enumerate(feeds):
        sim.set_input_tensor(i, nncase.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
    sim.run()
    n = sim.outputs_size() if callable(sim.outputs_size) else sim.outputs_size
    return [sim.get_output_tensor(i).to_numpy().copy() for i in range(n)]

rng = np.random.default_rng(11)
x = (rng.standard_normal((512, 1, 896)) * 0.3).astype(np.float32)
past = np.zeros((24, 2, 1, 0, 2, 64), np.float32)
a = run(sys.argv[1], [x, past])
b = run(sys.argv[2], [x, past])
print('nouts', len(a), len(b))
for i, (u, v) in enumerate(zip(a, b)):
    if u.shape == v.shape:
        d = np.abs(u.astype(np.float64) - v.astype(np.float64))
        print('out%d %s max=%.6f mean=%.8f' % (i, u.shape, d.max(), d.mean()))
    else:
        print('out%d SHAPE %s vs %s' % (i, u.shape, v.shape))
