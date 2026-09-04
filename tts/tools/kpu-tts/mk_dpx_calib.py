#!/usr/bin/env python3
# mk_dpx_calib.py (v2) — dp_x calibration + zero-pad semantic validation.
# The dynamic enc can only run at T=L (padded tokens break attention masks),
# so: run dynamic T=L, take /text_encoder/Transpose_output_0 [1,96,L],
# zero-pad to [1,96,64] for the static dp-only bucket.
# Also validates: dp_only(zero-pad dp_x) vs the dynamic full-model logw.
import glob
import os
import numpy as np
import onnx
import onnxruntime as ort

N = 64
OUT = '/tmp/kpu_poc/dpx_calib'
os.makedirs(OUT, exist_ok=True)

m = onnx.load('/tmp/vits-icefall-zh-aishell3/enc_dp_dynamic.onnx')
m.graph.output.append(onnx.helper.make_tensor_value_info(
    '/text_encoder/Transpose_output_0', onnx.TensorProto.FLOAT, [1, 96, 'T']))
sess = ort.InferenceSession(m.SerializeToString(), providers=['CPUExecutionProvider'])
s_only = ort.InferenceSession('/tmp/vits-icefall-zh-aishell3/dp_only.onnx',
                              providers=['CPUExecutionProvider'])

dirs = sorted(d for d in glob.glob('/tmp/kpu_poc/aishell3_calib/calib/*')
              if os.path.exists(d + '/vits6_tokens.bin'))
print('corpus sentences:', len(dirs))
worst = 0.0
for k, d in enumerate(dirs):
    tok = np.fromfile(d + '/vits6_tokens.bin', dtype=np.int64).reshape(1, -1)
    tl = np.fromfile(d + '/vits6_tokens_lens.bin', dtype=np.int64).reshape(1)
    spk = np.fromfile(d + '/vits6_speaker.bin', dtype=np.int64).reshape(1)
    L = int(tl[0])
    if L > N:
        print(f'  skip {k}: L={L} > {N}')
        continue
    feed = {'tokens': tok, 'tokens_lens': tl, 'speaker': spk,
            'noise_scale': np.array([0.667], np.float32),
            'alpha': np.array([1.0], np.float32),
            'noise_scale_dur': np.array([0.0], np.float32)}  # nsd=0: kill RNG
    logw_dyn, dp_x = sess.run(['/Mul_1_output_0',
                               '/text_encoder/Transpose_output_0'], feed)
    dp_x64 = np.zeros((1, 96, N), np.float32)
    dp_x64[:, :, :L] = dp_x
    tok64 = np.zeros((1, N), np.int64)
    tok64[:, :L] = tok
    out = s_only.run(None, {'dp_x': dp_x64, 'tokens': tok64,
                            'tokens_lens': tl, 'speaker': spk,
                            'noise_scale_dur': np.array([0.0], np.float32),
                            'alpha': np.array([1.0], np.float32)})[0]
    a = np.ceil(logw_dyn.flatten()[:L])
    b = np.ceil(out.flatten()[:L])
    d_tok = np.abs(a - b)
    worst = max(worst, float(d_tok.max()))
    print(f'  {k}: L={L} spk={int(spk[0])} maxdurD={d_tok.max():.0f} '
          f'flips={(d_tok > 0).sum()}/{L} >1frame={(d_tok > 1).sum()}')
    np.save(f'{OUT}/x_{k:03d}.npy', dp_x64)
    np.save(f'{OUT}/tok_{k:03d}.npy', tok64)
    np.save(f'{OUT}/tl_{k:03d}.npy', tl)
    np.save(f'{OUT}/spk_{k:03d}.npy', spk)
print(f'worst frame delta across corpus: {worst:.0f}')
print('saved to', OUT)
