#!/usr/bin/env python3
# mk_abc_calib.py — calibration sets for A (token stream) and C128 (windowed
# decoder inputs) from real sentences with the sherpa token convention.
import os
import glob
import numpy as np
import onnxruntime as ort

MDL = '/tmp/matcha-icefall-zh-baker'
OUTA = '/tmp/kpu_poc/matcha_calA'
OUTC = '/tmp/kpu_poc/matcha_calC'
os.makedirs(OUTA, exist_ok=True)
os.makedirs(OUTC, exist_ok=True)
XB, WB = 256, 128

lex = {}
for line in open(f'{MDL}/lexicon.txt', encoding='utf-8'):
    p = line.split()
    if len(p) >= 2:
        lex[p[0]] = p[1:]
t2i = {}
for line in open(f'{MDL}/tokens.txt', encoding='utf-8'):
    p = line.split()
    if len(p) >= 2:
        t2i[p[0]] = int(p[1])
words = sorted(lex.keys(), key=len, reverse=True)

PUNCTS = {'。': '.', '！': '!', '？': '?', '；': ';', '，': ','}

def toks_for(text):
    ids = []
    i = 0
    while i < len(text):
        hit = False
        for w in words:
            if text.startswith(w, i):
                ids.extend(t2i[s] for s in lex[w] if s in t2i)
                i += len(w)
                hit = True
                break
        if not hit:
            ch = text[i]
            if ch in PUNCTS and PUNCTS[ch] in t2i:
                ids.append(t2i[PUNCTS[ch]])
            i += 1
    if not ids:
        return ids
    return [1] + [v for t in ids for v in (t, 1)]  # AddBlank

TEXTS = [t for t in open('/mnt/d/work/git_dev/k230_prj/tmp/matcha_texts.txt',
                         encoding='utf-8').read().splitlines() if t.strip()]
print('texts:', len(TEXTS))

sA = ort.InferenceSession(f'{MDL}/matcha_A256.onnx', providers=['CPUExecutionProvider'])
na = nc = 0
for k, text in enumerate(TEXTS):
    ids = toks_for(text)
    if len(ids) < 4 or len(ids) > XB - 4:
        print(f'{k}: skip len={len(ids)}')
        continue
    x = np.full((1, XB), 1, np.int64)
    x[0, :len(ids)] = ids
    afeed = {'x': x, 'x_length': np.array([len(ids)], np.int64),
             'length_scale': np.array([1.0], np.float32)}
    dur, hid = sA.run(None, afeed)
    np.save(f'{OUTA}/x_{k:03d}.npy', x)
    # A calib: [x, x_length, length_scale] param-major
    # (saved separately; assembler builds the nested lists)

    # windows for C
    d = dur[0, :len(ids)]
    cum = np.cumsum(d)
    Lp = int(round(cum[-1]))
    oh = np.zeros((65536, XB), np.float32)  # big enough
    for n_i in range(len(ids)):
        lo = 0 if n_i == 0 else int(round(cum[n_i - 1]))
        oh[lo:int(round(cum[n_i])), n_i] = 1.0
    h_up = oh[:Lp] @ hid[0].T
    nw = 0
    for c0 in range(0, Lp, WB):
        f = min(WB, Lp - c0)
        if f < 16:
            break
        mm = np.zeros((1, WB, 80), np.float32)
        mm[0, :f] = h_up[c0:c0 + f]
        mask = np.zeros((1, 1, WB), np.float32)
        mask[0, 0, :f] = 1.0
        dur3 = np.zeros((1, 1, XB), np.float32)
        dur3[0, 0, :len(ids)] = d
        np.save(f'{OUTC}/mm_{nc:03d}.npy', mm)
        np.save(f'{OUTC}/mk_{nc:03d}.npy', mask)
        np.save(f'{OUTC}/du_{nc:03d}.npy', dur3)
        nc += 1
        nw += 1
    na += 1
    print(f'{k}: ids={len(ids)} Lp={Lp} windows={nw}')
print('A samples:', na, 'C windows:', nc)
