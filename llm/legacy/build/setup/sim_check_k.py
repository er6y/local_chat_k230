import nncase
import numpy as np

tag = 'l0_k'
km = open(f'/tmp/kpu_poc/qwen_s4sq/{tag}.kmodel', 'rb').read()
px = np.load(f'/tmp/kpu_poc/qwen_s4sq/{tag}.probe_x.npy')
py = np.load(f'/tmp/kpu_poc/qwen_s4sq/{tag}.probe_y.npy')

sim = nncase.Simulator()
sim.load_model(km)
sim.set_input_tensor(0, nncase.RuntimeTensor.from_numpy(px))
sim.run()
out = sim.get_output_tensor(0).to_numpy()
print('PC out shape:', out.shape, 'dtype:', out.dtype)
flat = out.flatten()
print('PC y[0..7] =', [f'{v:.4f}' for v in flat[:8]])
r = py.flatten()
print('ref y[0..7] =', [f'{v:.4f}' for v in r[:8]])
cos = float(np.dot(flat, r) / (np.linalg.norm(flat) * np.linalg.norm(r) + 1e-30))
print(f'cos={cos:.5f}')
# board saw: y[0..3] = 2.1502 1.3608 1.5663 -0.1423  -- search them in PC output
board = [2.1502, 1.3608, 1.5663, -0.1423]
for i in range(0, len(flat) - 4):
    if abs(flat[i] - board[0]) < 1e-3 and abs(flat[i+1] - board[1]) < 1e-3 \
       and abs(flat[i+2] - board[2]) < 1e-3 and abs(flat[i+3] - board[3]) < 1e-3:
        print(f'BOARD pattern found at PC flat index {i} '
              f'(out[{i//16}][{i%16}] in [1,1024,4,4] layout)')
        break
else:
    print('board pattern NOT found in PC output')
