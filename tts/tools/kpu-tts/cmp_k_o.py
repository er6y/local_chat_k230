#!/usr/bin/env python3
# cmp_k_o.py — spectral sanity of say-mode KPU (k1) vs corrected ORT (o1).
import wave
import numpy as np


def read_wav(p):
    w = wave.open(p)
    n = w.getnframes()
    x = np.frombuffer(w.readframes(n), np.int16).astype(np.float32) / 32768.0
    w.close()
    return x


def logmel(x, sr=22050, n_fft=1024, hop=256, n_mels=40):
    w = np.hanning(n_fft)
    frames = max(0, 1 + (len(x) - n_fft) // hop)
    spec = np.empty((frames, n_fft // 2), np.float32)
    for i in range(frames):
        spec[i] = np.abs(np.fft.rfft(x[i * hop:i * hop + n_fft] * w))[:n_fft // 2]
    mpts = np.linspace(0, 1, n_mels + 2)
    hz = mpts * (sr / 2)
    bin_i = np.floor((n_fft + 1) * hz / sr).astype(int)
    fb = np.zeros((n_mels, spec.shape[1]), np.float32)
    for j in range(1, n_mels + 1):
        l, c, r = bin_i[j - 1], bin_i[j], bin_i[j + 1]
        for b in range(l, c):
            if 0 <= b < fb.shape[1]:
                fb[j - 1, b] = (b - l) / max(1, c - l)
        for b in range(c, r):
            if 0 <= b < fb.shape[1]:
                fb[j - 1, b] = (r - b) / max(1, r - c)
    return np.log(spec @ fb.T + 1e-9)


for tag in ('matcha_k1', 'matcha_o1'):
    x = read_wav(f'/tmp/{tag}.wav')
    rms = np.sqrt((x ** 2).mean())
    m = logmel(x)
    # spectral flux profile (speech has structured modulation)
    flux = np.abs(np.diff(m, axis=0)).mean()
    print(f'{tag}: dur={len(x)/22050.0:.2f}s rms={rms:.4f} '
          f'active_frac={float((np.abs(x)>0.01).mean()):.2f} '
          f'melrange=[{m.min():.1f},{m.max():.1f}] flux={flux:.3f}')

k = logmel(read_wav('/tmp/matcha_k1.wav'))
o = logmel(read_wav('/tmp/matcha_o1.wav'))
kk = min(len(k), len(o))
a, b = k[:kk].ravel(), o[:kk].ravel()
print('spec cos k1 vs o1:', float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b))))
