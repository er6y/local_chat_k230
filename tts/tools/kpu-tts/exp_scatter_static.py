#!/usr/bin/env python3
# exp_scatter_static.py — is flows.X/ScatterND_1_output_0 static? And does
# rewiring canvas/softplus9 to Pad_output_0 keep the canvas bit-exact?
import numpy as np, onnxruntime as ort, onnx

N = 64
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

def feed_pruned(tok):
    return {'tokens': tok,
            'tokens_lens': np.array([N], dtype=np.int64),
            'speaker': np.array([10], dtype=np.int64),
            'noise_scale_dur': np.array([0.8], dtype=np.float32),
            'alpha': np.array([1.0], dtype=np.float32)}

cases = [('/tmp/vits-icefall-zh-aishell3/probe_mu.npy',
          '/tmp/vits-icefall-zh-aishell3/probe_tokens.npy')]
for k in range(3):
    cases.append((f'/tmp/vits-icefall-zh-aishell3/synth_mu{k}.npy',
                  f'/tmp/vits-icefall-zh-aishell3/synth_tok{k}.npy'))

ext = []
for f in (7, 5, 3):
    ext += [f'/duration_predictor/flows.{f}/ScatterND_output_0',
            f'/duration_predictor/flows.{f}/ScatterND_1_output_0',
            f'/duration_predictor/flows.{f}/Pad_output_0',
            f'/duration_predictor/flows.{f}/canvas/softplus9']

s2 = ort.InferenceSession(NEW, providers=['CPUExecutionProvider'])
_pm = onnx.load('/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_pruned.onnx')
for name in ext:
    vi = onnx.helper.make_tensor_value_info(name, onnx.TensorProto.FLOAT, None)
    _pm.graph.output.append(vi)
_pmp = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_pruned_vi.onnx'
onnx.save(_pm, _pmp)
s2b = ort.InferenceSession(_pmp, providers=['CPUExecutionProvider'])

ref_outs = []
scatter_vals = {name: [] for name in ext}
for i, (mp, tp) in enumerate(cases):
    mu, tok = load_input(mp, tp)
    ref_outs.append(s2.run(None, feed(mu, tok))[0].flatten())
    outs = s2b.run(ext, feed_pruned(tok))
    for name, v in zip(ext, outs):
        scatter_vals[name].append(v.flatten())

print('== static-ness of ScatterND chain (max abs diff vs case 0) ==')
for name in ext:
    d = [float(np.abs(scatter_vals[name][0] - v).max()) for v in scatter_vals[name][1:]]
    print(f'{name}: {max(d):.6g}  {"STATIC" if max(d) == 0 else "DYNAMIC"}')

# Rewire experiment: canvas/softplus9 <- Pad_output_0 (per flow)
m = onnx.load(NEW)
for n in m.graph.node:
    if n.name.endswith('/canvas/softplus9'):
        for j, i in enumerate(n.input):
            if 'ScatterND_1_output_0' in i:
                n.input[j] = i.replace('ScatterND_1_output_0', 'Pad_output_0')
onnx.save(m, '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_rewire.onnx')
s3 = ort.InferenceSession('/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_rewire.onnx',
                          providers=['CPUExecutionProvider'])
print()
print('== rewire softplus9<-Pad_output_0: final output vs canvas ==')
for i, (mp, tp) in enumerate(cases):
    mu, tok = load_input(mp, tp)
    c = s3.run(None, feed(mu, tok))[0].flatten()
    a, b = ref_outs[i], c
    cos = float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-30))
    mad = float(np.abs(a - b).max())
    print(f'case {i}: cos={cos:.9f} max_abs_diff={mad:.6g}')
