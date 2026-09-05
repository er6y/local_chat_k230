#!/usr/bin/env python3
# sim_say_tokens.py — replicate the runner's greedy longest-match and dump
# the phone sequence for the test texts; every non-punct char must be covered.
MDL = '/tmp/matcha-icefall-zh-baker'
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
words_sorted = sorted(lex.keys(), key=len, reverse=True)
print('max word len (bytes):', len(words_sorted[0].encode()), words_sorted[0])

def say_tokenize(text):
    ids = []
    syls = []
    i = 0
    while i < len(text):
        matched = False
        for w in words_sorted:
            if text.startswith(w, i):
                syls.extend(lex[w])
                for s in lex[w]:
                    if s in tok2id:
                        ids.append(tok2id[s])
                i += len(w)
                matched = True
                break
        if not matched:
            ch = text[i]
            if ch in '。！？；':
                if ids:
                    yield ids, syls
                    ids, syls = [], []
            # skip other chars (commas etc.)
            i += 1
    if ids:
        yield ids, syls

for text in ('今天天气真好，我们去公园散步吧。晚上一起吃饭，然后看个电影。',
             '好的，马上为您播放音乐。我来为您放一首轻音乐，再看看这个乐队。'):
    print('TEXT:', text)
    for k, (ids, syls) in enumerate(say_tokenize(text)):
        print(f'  sent {k}: {len(ids)} tokens: {" ".join(syls)}')
