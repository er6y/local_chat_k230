import struct, re
data = open('/mnt/data/static/llm_kv6_stacked.kmodel', 'rb').read()
ident, ver, flags, align, modules, emod, efun, rsv = struct.unpack_from('<8I', data, 0)
pos = 32
kfuncs = []
for m in range(modules):
    mstart = pos
    kind = data[pos:pos+16].rstrip(b'\x00').decode(errors='replace')
    mver, msec, mfun, mrsv, msize = struct.unpack_from('<IIIIQ', data, pos+16)
    pos += 40
    recs = []
    for f in range(mfun):
        params, fsec, entry, textsz, fsize = struct.unpack_from('<IIQQQ', data, pos)
        recs.append((params, entry, textsz))
        pos += fsize if fsize >= 32 else 32
    tbase = None
    for s in range(msec):
        epos = pos
        name = data[pos:pos+16].rstrip(b'\x00').decode(errors='replace')
        sflags, srsv, ssize, sbs, sbsz, smem = struct.unpack_from('<IIQQQQ', data, pos+16)
        if name.startswith('.text') and kind != 'stackvm':
            tbase = epos + 56 + sbs
        pos = epos + ssize
    if kind != 'stackvm' and tbase is not None:
        kfuncs = [(tbase + entry, tsz, params) for (params, entry, tsz) in recs]
    pos = mstart + msize
txt = open('/mnt/data/static/prof_kv6q2cpu.txt', encoding='utf-8', errors='replace').read()
blocks = re.split(r'stack OPs timeline', txt)
durs = []
for ln in blocks[2].splitlines():
    m = re.match(r'\|EXTCALL\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|', ln)
    if m:
        durs.append(float(m.group(3)))
n = min(len(durs), len(kfuncs))
fasts = [i for i in range(n) if durs[i] < 0.1 and kfuncs[i][1] == 113928]
slows = [i for i in range(n) if durs[i] >= 2.0 and kfuncs[i][1] == 113928]
print('fast113928 sites:', fasts, 'slow113928 sites:', slows)
a, b = fasts[0], slows[0]
ab = data[kfuncs[a][0]:kfuncs[a][0]+113928]
bb = data[kfuncs[b][0]:kfuncs[b][0]+113928]
diff = [j for j in range(113928) if ab[j] != bb[j]]
print('byte diffs: %d / 113928' % len(diff))
if diff:
    print('first 40 diffs at offsets:', diff[:40])
    # group into runs
    runs = []
    s = diff[0]; p = diff[0]
    for j in diff[1:]:
        if j == p + 1:
            p = j
        else:
            runs.append((s, p)); s = j; p = j
    runs.append((s, p))
    print('diff runs: %d' % len(runs))
    for s, e in runs[:20]:
        print('  off %6d-%6d: fast=%s slow=%s' % (s, e, ab[s:min(e+9, s+33)].hex(), bb[s:min(e+9, s+33)].hex()))
# also compare params field of function records
print('params fast=%s slow=%s' % (kfuncs[a][2], kfuncs[b][2]))
