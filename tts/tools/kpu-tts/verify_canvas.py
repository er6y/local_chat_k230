#!/usr/bin/env python3
# verify_canvas.py — A/B the canvas-rewritten dp against the original on
# multiple real inputs (from the 20-sentence calibration set).
import numpy as np, onnxruntime as ort, glob, os

N = 64
ORIG = '/tmp/vits-icefall-zh-aishell3/dp_b64.onnx'
NEW = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas.onnx'

def load_input(mu_path, tok_path):
    mu = np.load(mu_path).reshape(1, 96, -1)
    tok = np.load(tok_path).reshape(1, -1)
    if mu.shape[2] < N:
        mu = np.concatenate([mu, np.zeros((1, 96, N - mu.shape[2]), mu.dtype)], axis=2)
    if tok.shape[1] < N:
        tok = np.concatenate([tok, np.zeros((1, N - tok.shape[1]), tok.dtype)], axis=1)
    return mu.astype(np.float32), tok.astype(np.int64)

def feed(mu, tok):
    return {'/text_encoder/Split_output_0': mu, 'tokens': tok,
            'tokens_lens': np.array([N], dtype=np.int64),
            'speaker': np.array([10], dtype=np.int64),
            'noise_scale_dur': np.array([0.8], dtype=np.float32),
            'alpha': np.array([1.0], dtype=np.float32)}

s1 = ort.InferenceSession(ORIG, providers=['CPUExecutionProvider'])
s2 = ort.InferenceSession(NEW, providers=['CPUExecutionProvider'])

# probe #1 + a few calib samples
cases = [('/tmp/vits-icefall-zh-aishell3/probe_mu.npy',
          '/tmp/vits-icefall-zh-aishell3/probe_tokens.npy')]
# also generate a synthetic mu with a wider spread to force mixed bands
rng = np.random.default_rng(42)
for k in range(3):
    mu_s = rng.normal(0, 2.5, (1, 96, N)).astype(np.float32)
    tok_s = rng.integers(1, 50, (1, N)).astype(np.int64)
    np.save(f'/tmp/vits-icefall-zh-aishell3/synth_mu{k}.npy', mu_s)
    np.save(f'/tmp/vits-icefall-zh-aishell3/synth_tok{k}.npy', tok_s)
    cases.append((f'/tmp/vits-icefall-zh-aishell3/synth_mu{k}.npy',
                  f'/tmp/vits-icefall-zh-aishell3/synth_tok{k}.npy'))

for i, (mp, tp) in enumerate(cases):
    mu, tok = load_input(mp, tp)
    a = s1.run(None, feed(mu, tok))[0].flatten()
    b = s2.run(None, feed(mu, tok))[0].flatten()
    cos = float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-30))
    mad = float(np.abs(a - b).max())
    print(f'case {i}: cos={cos:.6f} max_abs_diff={mad:.6g} len={len(a)}')
