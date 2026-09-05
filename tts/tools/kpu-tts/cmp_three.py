#!/usr/bin/env python3
# cmp_three.py — same-mel 3-way: x86 ORT gold vs board ORT vs board KPU.
import wave
import numpy as np
import onnxruntime as ort


def read_wav(p):
    w = wave.open(p)
    n = w.getnframes()
    x = np.frombuffer(w.readframes(n), np.int16).astype(np.float64) / 32768.0
    w.close()
    return x


mel = np.fromfile('/tmp/matcha_d.wav.mel.f32', np.float32)
Lm = mel.size // 80
mel = mel.reshape(80, Lm)
sv = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/hifigan_v2.onnx',
                          providers=['CPUExecutionProvider'])
gold = sv.run(['audio'], {'mel': mel.reshape(1, 80, Lm)})[0].ravel().astype(np.float64)

kpu = read_wav('/tmp/matcha_d.wav')       # chunked KPU (fades applied)
bo = read_wav('/tmp/matcha_d.wav.ort.wav')  # board ORT, same mel


def cos(a, b):
    k = min(len(a), len(b))
    a, b = a[:k], b[:k]
    return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-30))


print(f'Lm={Lm} gold={gold.size} bo={bo.size} kpu={kpu.size}')
print('board-ORT vs x86-gold   cos =', round(cos(bo, gold), 4))
print('KPU-chunk vs x86-gold   cos =', round(cos(kpu, gold), 4))
print('KPU-chunk vs board-ORT  cos =', round(cos(kpu, bo), 4))
# skip the crossfaded head of chunk 2 for a cleaner chunk-0 comparison
n0 = min(128 * 256, len(gold), len(kpu))
print('chunk-0 only (first 128 frames): KPU vs gold cos =',
      round(cos(kpu[:n0], gold[:n0]), 4),
      ' board-ORT vs gold =', round(cos(bo[:n0], gold[:n0]), 4))
