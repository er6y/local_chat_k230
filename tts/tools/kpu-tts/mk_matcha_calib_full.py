#!/usr/bin/env python3
# mk_matcha_calib_full.py — FULL-BUCKET calibration mels: concatenate texts so
# every calib sample's mel fills the whole 512-frame bucket with real content
# (the -12 silence pad dominated 83% of the previous calibration statistics and
# crushed the real region's int8 precision).
import os
import numpy as np
import onnxruntime as ort

MDL = '/tmp/matcha-icefall-zh-baker'
OUT = '/tmp/kpu_poc/matcha_calib_full'
os.makedirs(OUT, exist_ok=True)
BUCKET = 512

lex = {}
for line in open(f'{MDL}/lexicon.txt', encoding='utf-8'):
    parts = line.split()
    if len(parts) >= 2:
        lex[parts[0]] = parts[1:]
tok2id = {}
for line in open(f'{MDL}/tokens.txt', encoding='utf-8'):
    parts = line.split()
    if len(parts) >= 2:
        tok2id[parts[0]] = int(parts[1])

def text_to_ids(text):
    ids = []
    for ch in text:
        if ch in lex:
            for syl in lex[ch]:
                if syl in tok2id:
                    ids.append(tok2id[syl])
    return ids

TEXTS = [
    '今天天气真好，我们去公园散步吧。',
    '晚上一起吃饭，然后看个电影。',
    '好的，马上为您播放音乐。',
    '我来为您放一首轻音乐，再看看这个乐队。',
    '这个假期我想去重庆旅游，看看长江。',
    '早上我带着背包从首都坐火车出发。',
    '路过学校时教室里传来弹琴的声音。',
    '晚上睡觉前我处理完邮件就休息了。',
    '虽然行李沉重但一想到演出就兴奋。',
    '这一路空气质量很好心情也放松了。',
    '您好，请问有什么可以帮您？',
    '我会为您查询天气和设置提醒。',
    '明天的会议改到下午三点开始了。',
    '请帮我打开客厅的灯和空调。',
    '谢谢您，祝您生活愉快再见。',
    '人工智能正在改变我们的生活方式。',
    '语音助手可以回答问题播放音乐。',
    '深圳今天多云转晴气温二十八度。',
    '我们支持中英文混合的语音输入。',
    '这是一个测试句子用来校准模型。',
]

# build long token sequences: each ~45-60 tokens -> mel ~500-700 frames,
# then TRUNCATE the mel to the first 512 frames (all real content)
sa = ort.InferenceSession(f'{MDL}/model-steps-3.onnx', providers=['CPUExecutionProvider'])
rng = np.random.default_rng(7)
n_ok = 0
for k in range(20):
    ids = []
    while len(ids) < 55:
        t = TEXTS[int(rng.integers(0, len(TEXTS)))]
        ids.extend(text_to_ids(t))
    x = np.array([ids], np.int64)
    mel = sa.run(None, {'x': x, 'x_length': np.array([len(ids)], np.int64),
                        'noise_scale': np.array([0.667], np.float32),
                        'length_scale': np.array([1.0], np.float32)})[0]
    L = mel.shape[2]
    if L < BUCKET:
        print(f'{k}: tokens={len(ids)} mel L={L} < {BUCKET}, skip')
        continue
    mel_full = mel[:, :, :BUCKET].copy()
    np.save(f'{OUT}/mel_{k:03d}.npy', mel_full)
    n_ok += 1
    print(f'{k}: tokens={len(ids)} mel L={L} -> full bucket, '
          f'range [{mel_full.min():.2f},{mel_full.max():.2f}]')
print(f'saved {n_ok} full-bucket mels to {OUT}')
