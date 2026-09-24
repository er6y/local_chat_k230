import sys
import nncase
import numpy as np

d = sys.argv[1] if len(sys.argv) > 1 else '/tmp/kpu_poc/qwen_s4sq_29'
tags = ['l0_q', 'l0_k', 'l0_v', 'l0_o', 'l0_gate', 'l0_up', 'l0_down']
ok = 0
for tag in tags:
    try:
        km = open(f'{d}/{tag}.kmodel', 'rb').read()
        px = np.load(f'{d}/{tag}.probe_x.npy')
        py = np.load(f'{d}/{tag}.probe_y.npy')
        sim = nncase.Simulator()
        sim.load_model(km)
        sim.set_input_tensor(0, nncase.RuntimeTensor.from_numpy(px))
        sim.run()
        out = sim.get_output_tensor(0).to_numpy()
        flat = out.flatten().astype(np.float64)
        r = py.flatten().astype(np.float64)
        cos = float(np.dot(flat, r) / (np.linalg.norm(flat) * np.linalg.norm(r) + 1e-30))
        status = 'PASS' if cos > 0.98 else 'FAIL'
        ok += cos > 0.98
        print(f'{tag:10s} cos={cos:.5f} {status}  y[0..3]={[f"{v:.4f}" for v in flat[:4]]}')
    except Exception as e:
        print(f'{tag:10s} ERROR: {e}')
print(f'== {ok}/{len(tags)} pass ==')
