#!/usr/bin/env python3
# mk_probe_bins.py — raw probe files (x/tok/tl/spk) for corpus 0,5,8 + dynamic gold.
import glob
import os
import numpy as np
import onnx
import onnxruntime as ort

m = onnx.load('/tmp/vits-icefall-zh-aishell3/enc_dp_dynamic.onnx')
s = ort.InferenceSession(m.SerializeToString(), providers=['CPUExecutionProvider'])
dirs = sorted(d for d in glob.glob('/tmp/kpu_poc/aishell3_calib/calib/*')
              if os.path.exists(d + '/vits6_tokens.bin'))
os.makedirs('/tmp/kpu_poc/probe_dpx', exist_ok=True)
for k in (0, 5, 8):
    d = dirs[k]
    tok = np.fromfile(d + '/vits6_tokens.bin', np.int64).reshape(1, -1)
    tl = np.fromfile(d + '/vits6_tokens_lens.bin', np.int64).reshape(1)
    spk = np.fromfile(d + '/vits6_speaker.bin', np.int64).reshape(1)
    L = int(tl[0])
    x = np.load(f'/tmp/kpu_poc/dpx_calib/x_{k:03d}.npy')
    tok64 = np.zeros((1, 64), np.int64)
    tok64[:, :L] = tok
    gold = s.run(['/Mul_1_output_0'],
                 {'tokens': tok, 'tokens_lens': tl, 'speaker': spk,
                  'noise_scale': np.array([0.667], np.float32),
                  'alpha': np.array([1.0], np.float32),
                  'noise_scale_dur': np.array([0.0], np.float32)})[0].flatten()[:L]
    np.ascontiguousarray(x, np.float32).flatten().tofile(f'/tmp/kpu_poc/probe_dpx/x{k}.bin')
    tok64.flatten().tofile(f'/tmp/kpu_poc/probe_dpx/tok{k}.bin')
    tl.tofile(f'/tmp/kpu_poc/probe_dpx/tl{k}.bin')
    spk.tofile(f'/tmp/kpu_poc/probe_dpx/spk{k}.bin')
    np.save(f'/tmp/kpu_poc/probe_dpx/gold{k}.npy', gold)
    print(k, 'L=', L, 'gold[:4]=', np.round(gold[:4], 2), 'gold[-3:]=', np.round(gold[-3:], 2))
