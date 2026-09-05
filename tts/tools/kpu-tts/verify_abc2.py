#!/usr/bin/env python3
# verify_abc2.py — A + B(numpy: mm/dur/mask) + C v3 vs original, ns=0.
import numpy as np
import onnxruntime as ort

sO = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/model-steps-3.onnx',
                          providers=['CPUExecutionProvider'])
sA = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_A256.onnx',
                          providers=['CPUExecutionProvider'])
sC = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C512.onnx',
                          providers=['CPUExecutionProvider'])
XB, MB = 256, 512

rng = np.random.default_rng(11)
tests = []
tok0 = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
tests.append(('short', np.asarray([1] + [v for t in tok0 for v in (t, 1)], np.int64)))
tests.append(('long', np.asarray([1] + [v for t in rng.integers(4, 2000, 40) for v in (int(t), 1)], np.int64)))

ok_all = True
for tag, ids in tests:
    L = len(ids)
    x_pad = np.full((1, XB), 1, np.int64)
    x_pad[0, :L] = ids
    feed = {'x': ids[None, :], 'x_length': np.array([L], np.int64),
            'noise_scale': np.array([0.0], np.float32),
            'length_scale': np.array([1.0], np.float32)}
    mel_orig = sO.run(['mel'], feed)[0]

    afeed = {'x': x_pad, 'x_length': np.array([L], np.int64),
             'length_scale': np.array([1.0], np.float32)}
    for i in sA.get_inputs():
        if i.name == 'noise_scale':
            afeed[i.name] = np.array([0.0], np.float32)
    dur, hid = sA.run(None, afeed)

    d = dur[0, :L]
    cum = np.cumsum(d)
    Lp = int(round(cum[-1]))
    oh = np.zeros((MB, XB), np.float32)
    for n_i in range(L):
        lo = 0 if n_i == 0 else int(round(cum[n_i - 1]))
        hi = int(round(cum[n_i]))
        oh[lo:hi, n_i] = 1.0
    mm = (oh @ hid[0].T)[None, :, :].astype(np.float32)
    dur3 = np.zeros((1, 1, XB), np.float32)
    dur3[0, 0, :L] = d
    mask = (np.arange(MB) < Lp).astype(np.float32)[None, None, :]

    cfeed = {'/MatMul_output_0': mm, '/Mul_1_output_0': dur3,
             '/Cast_3_output_0': mask, 'noise_scale': np.array([0.0], np.float32)}
    mel_c = sC.run(None, cfeed)[0]
    k = min(Lp, mel_orig.shape[2], mel_c.shape[2])
    diff = np.abs(mel_c[0, :, :k] - mel_orig[0, :, :k])
    ok = diff.max() < 5e-3 and Lp == mel_orig.shape[2]
    ok_all &= ok
    print(f'{tag}: L={L} Lp={Lp} orig={mel_orig.shape[2]} '
          f'maxdiff={diff.max():.6f} mean={diff.mean():.6f} -> {"OK" if ok else "FAIL"}')
print('ALL OK' if ok_all else 'SOME FAIL')
