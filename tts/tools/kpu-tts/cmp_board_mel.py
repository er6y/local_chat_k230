#!/usr/bin/env python3
# cmp_board_mel.py — board riscv acoustic mel vs x86 mels (same tokens,
# noise makes runs differ; check the board mel is within the x86 family).
import numpy as np
import onnxruntime as ort

board = np.fromfile('/tmp/board_mel.f32', np.float32).reshape(80, 512)[:, :88]

tok = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
L = len(tok)
sa = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/model-steps-3.onnx',
                          providers=['CPUExecutionProvider'])
x86 = []
for k in range(5):
    out = sa.run(['mel'], {'x': tok.reshape(1, -1), 'x_length': np.array([L], np.int64),
                           'noise_scale': np.array([0.667], np.float32),
                           'length_scale': np.array([1.0], np.float32)})[0]
    x86.append(out[0][:, :88])  # crop to common frames

def cos(a, b):
    a = a.astype(np.float64).ravel(); b = b.astype(np.float64).ravel()
    return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b)))

xx = [cos(x86[i], x86[j]) for i in range(5) for j in range(i + 1, 5)]
bx = [cos(board, m) for m in x86]
print('x86-vs-x86 cos: min=%.4f mean=%.4f' % (min(xx), sum(xx) / len(xx)))
print('board-vs-x86 cos: %s' % [round(v, 4) for v in bx])
print('board std=%.2f, x86 stds=%s' % (board.std(), [round(m.std(), 2) for m in x86]))
