#!/usr/bin/python3
"""gen_vendor_testdata.py - Generate test data for t_vendor (Path B M1)
Runs one kmodel via nncaseruntime (same as daemon), saves input + output
to .bin files for t_vendor to compare against.
"""
import numpy as np
import nncaseruntime as nn
import sys
import os

# Use l0_q S4 kmodel: K=1024, OUT=2048
KMODEL = "/mnt/data/kpu_qwen/s4sq_q8/l0_q.kmodel"
SCALE = "/mnt/data/kpu_qwen/s4sq_q8/l0_q.scale"
OUTDIR = "/mnt/data/kpu_llm"

def main():
    mt = 4  # S=4, so MT=16, pad 4 rows
    S = 4
    MT = S * S  # 16

    interp = nn.Interpreter()
    interp.load_model(open(KMODEL, "rb").read())
    in_shape = [int(v) for v in interp.get_input_shape(0)]
    out_shape = [int(v) for v in interp.get_output_shape(0)]
    K = in_shape[1]
    OUT = out_shape[1]
    print(f"kmodel: {KMODEL}")
    print(f"in_shape={in_shape} out_shape={out_shape} K={K} OUT={OUT} mt={mt} MT={MT}")

    # Generate deterministic test input
    np.random.seed(42)
    x = np.random.randn(mt, K).astype(np.float32)

    # Fold: x[mt,K] -> in[1,K,S,S] with SmoothQuant
    sc = None
    if os.path.exists(SCALE):
        sc = np.fromfile(SCALE, dtype=np.float32)
        print(f"scale: {sc.shape}")

    inp = np.zeros((1, K, MT), dtype=np.float32)
    inp[0, :, :mt] = x.T  # [K, mt]
    if sc is not None:
        inp[0, :, :mt] /= sc.reshape(K, 1)
    inp = inp.reshape(1, K, S, S)

    # Run
    t = nn.RuntimeTensor.from_numpy(inp)
    interp.set_input_tensor(0, t)
    interp.run()
    out = interp.get_output_tensor(0).to_numpy()  # [1, OUT, S, S]

    # Extract y[mt, OUT]
    y = out.reshape(OUT, MT)[:, :mt].T.copy()  # [mt, OUT]

    # Save
    x_path = os.path.join(OUTDIR, "vendor_test_input.bin")
    y_path = os.path.join(OUTDIR, "vendor_test_ref_output.bin")
    x.astype(np.float32).tofile(x_path)
    y.astype(np.float32).tofile(y_path)
    print(f"saved input: {x_path} ({x.size} floats)")
    print(f"saved ref:   {y_path} ({y.size} floats)")

    # Also save the folded input for debugging
    inp_path = os.path.join(OUTDIR, "vendor_test_folded_input.bin")
    inp.astype(np.float32).tofile(inp_path)
    print(f"saved folded: {inp_path} ({inp.size} floats)")

    print(f"\nTo test: ./t_vendor {KMODEL} {x_path} {y_path}")

if __name__ == "__main__":
    main()
