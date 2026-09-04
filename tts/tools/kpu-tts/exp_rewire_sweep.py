#!/usr/bin/env python3
# exp_rewire_sweep.py — strict sweep: canvas vs softplus9<-Pad rewire on 20
# diverse inputs (real calib tokens + synthetic mu of varying spread).
import numpy as np, onnxruntime as ort, onnx, glob, os

N = 64
NEW = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas.onnx'
REW = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_rewire.onnx'

s2 = ort.InferenceSession(NEW, providers=['CPUExecutionProvider'])
s3 = ort.InferenceSession(REW, providers=['CPUExecutionProvider'])

def feed(mu, tok):
    return {'/text_encoder/Split_output_0': mu, 'tokens': tok,
            'tokens_lens': np.array([N], np.int64),
            'speaker': np.array([10], np.int64),
            'noise_scale_dur': np.array([0.8], np.float32),
            'alpha': np.array([1.0], np.float32)}

# real calibration mu/tokens if present
mu_files = sorted(glob.glob('/tmp/vits-icefall-zh-aishell3/dp_calib*/*mu*.npy'))
mu_files += sorted(glob.glob('/tmp/vits-icefall-zh-aishell3/enc_calib*/*mu*.npy'))
mu_files += sorted(glob.glob('/tmp/kpu_poc/enc_calib_b64/mu_*.npy'))

rng = np.random.default_rng(2026)
cases = []
# 1) real calib inputs
for mp in mu_files[:10]:
    mu = np.load(mp).reshape(1, 96, -1).astype(np.float32)
    if mu.shape[2] < N:
        mu = np.concatenate([mu, np.zeros((1, 96, N - mu.shape[2]), mu.dtype)], 2)
    tok = rng.integers(1, 50, (1, N)).astype(np.int64)
    cases.append((mu[:, :, :N], tok, mp))
# 2) synthetic spreads to force different band mixes
for sig in (0.02, 0.1, 0.4, 0.8, 1.5, 3.0, 8.0, 25.0):
    mu = rng.normal(0.0, sig, (1, 96, N)).astype(np.float32)
    tok = rng.integers(1, 50, (1, N)).astype(np.int64)
    cases.append((mu, tok, f'synth_sigma{sig}'))
# 3) extreme constant-ish values
mu = (rng.normal(0, 1, (1, 96, N)) * 0 + 4.7).astype(np.float32)
cases.append((mu, rng.integers(1, 50, (1, N)).astype(np.int64), 'const_4.7'))
mu = (rng.normal(0, 1, (1, 96, N)) * 0 - 6.0).astype(np.float32)
cases.append((mu, rng.integers(1, 50, (1, N)).astype(np.int64), 'const_-6'))

bad = 0
for i, (mu, tok, tag) in enumerate(cases):
    a = s2.run(None, feed(mu, tok))[0].flatten()
    b = s3.run(None, feed(mu, tok))[0].flatten()
    eq = np.array_equal(a, b)
    if not eq:
        bad += 1
        print(f'case {i} ({tag}): MISMATCH maxdiff={np.abs(a - b).max():.6g}')
    else:
        print(f'case {i} ({tag}): bit-exact')
print(f'\n{bad}/{len(cases)} mismatches')
