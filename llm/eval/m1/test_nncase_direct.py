#!/usr/bin/python3
"""test_nncase_direct.py - directly invoke nncaseruntime (no socket) to verify
GNNE hardware works. This is the same path the daemon uses."""
import numpy as np
import nncaseruntime as nn
import os, sys

KMODEL = "/mnt/data/kpu_qwen/s4sq_q8/l0_q.kmodel"
SCALE = "/mnt/data/kpu_qwen/s4sq_q8/l0_q.scale"

def main():
    mt = 4
    S = 4
    MT = S * S

    interp = nn.Interpreter()
    interp.load_model(open(KMODEL, "rb").read())
    in_shape = [int(v) for v in interp.get_input_shape(0)]
    out_shape = [int(v) for v in interp.get_output_shape(0)]
    K = in_shape[1]
    OUT = out_shape[1]
    print(f"in={in_shape} out={out_shape} K={K} OUT={OUT}")

    np.random.seed(42)
    x = np.random.randn(mt, K).astype(np.float32)

    sc = np.fromfile(SCALE, dtype=np.float32) if os.path.exists(SCALE) else None
    inp = np.zeros((1, K, MT), dtype=np.float32)
    inp[0, :, :mt] = x.T
    if sc is not None:
        inp[0, :, :mt] /= sc.reshape(K, 1)
    inp = inp.reshape(1, K, S, S)

    t = nn.RuntimeTensor.from_numpy(inp)
    interp.set_input_tensor(0, t)
    print("running...")
    interp.run()
    out = interp.get_output_tensor(0).to_numpy()
    y = out.reshape(OUT, MT)[:, :mt].T.copy()

    print(f"y[0:4,0:4]:")
    print(y[:4, :4])
    print(f"y nonzero: {np.count_nonzero(y)} / {y.size}")
    print(f"y max: {np.abs(y).max():.4f}")

if __name__ == "__main__":
    main()
