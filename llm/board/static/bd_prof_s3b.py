# board: profile specimen B (kall/vall stacked host inputs, in-graph Gather->GEMM)
import sys, time
import numpy as np
import nncaseruntime as nn

KM = "/mnt/data/kmini/s3b.kmodel"
itp = nn.Interpreter()
itp.load_model(open(KM, "rb").read())
rng = np.random.default_rng(9)
x = (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32)
ka = (rng.standard_normal((2, 64, 32)) * 0.2).astype(np.float32)
va = (rng.standard_normal((2, 32, 64)) * 0.2).astype(np.float32)
itp.set_profiling(True)
for i, t in enumerate([x, ka, va]):
    itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
r = itp.run()
out = itp.get_output_tensor(0).to_numpy()
print("out norm=%.4f" % float(np.sqrt((out * out).sum())), flush=True)
for k in range(2):
    t0 = time.time()
    itp.run()
    print("run%d %.4fs" % (k, time.time() - t0), flush=True)
print("PROFS3B_DONE", flush=True)
