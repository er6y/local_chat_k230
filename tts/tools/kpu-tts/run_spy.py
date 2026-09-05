#!/usr/bin/env python3
# run_spy.py — capture the OFFICIAL frontend token stream via spy models.
import numpy as np
import sherpa_onnx
from sherpa_onnx import OfflineTtsConfig, OfflineTtsModelConfig, OfflineTtsMatchaModelConfig

MDL = '/tmp/matcha-icefall-zh-baker'
mm = OfflineTtsMatchaModelConfig(
    acoustic_model=f'{MDL}/spy_acoustic.onnx',
    vocoder=f'{MDL}/spy_vocoder.onnx',
    lexicon=f'{MDL}/lexicon.txt',
    tokens=f'{MDL}/tokens.txt',
    dict_dir=f'{MDL}/dict',
)
cfg = OfflineTtsConfig(model=OfflineTtsModelConfig(matcha=mm, num_threads=1))
tts = sherpa_onnx.OfflineTts(cfg)
audio = tts.generate('今天天气真好，我们去公园散步吧。')
ids = np.asarray(audio.samples, np.float32)
nz = ids[np.abs(ids) > 0.5].astype(int)
print('raw samples:', len(ids), 'nonzero ids:', nz.tolist())

# my stream for comparison (with punct tokens): from test_punct_tokens
mine = [705, 1645, 1645, 1293, 1965, 597, 2, 1737, 1004, 1343, 525, 1876, 1448, 113, 34, 3]
print('mine       :', mine)
# decode official ids to tokens
id2tok = {}
for line in open(f'{MDL}/tokens.txt'):
    parts = line.split()
    if len(parts) >= 2:
        id2tok[int(parts[1])] = parts[0]
print('official decoded:', [id2tok.get(i, '?') for i in nz.tolist()])
