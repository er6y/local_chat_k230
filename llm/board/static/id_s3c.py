import struct
import re
from collections import Counter

KM = '/mnt/data/kmini/s3c.kmodel'
PROF = '/mnt/data/kmini/prof_s3c.txt'
data = open(KM, 'rb').read()
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
        kfuncs = [(params, entry, tsz) for (params, entry, tsz) in recs]
    pos = mstart + msize
txt = open(PROF, encoding='utf-8', errors='replace').read()
blocks = re.split(r'stack OPs timeline', txt)
durs = []
for ln in blocks[2].splitlines():
    mm = re.match(r'\|EXTCALL\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|', ln)
    if mm:
        durs.append(float(mm.group(3)))
n = min(len(durs), len(kfuncs))
print('funcs=%d durs=%d' % (len(kfuncs), len(durs)))
for i in range(n):
    d = durs[i]
    if d >= 0.4:
        params, entry, tsz = kfuncs[i]
        print('idx=%d dur=%.3f tsz=%d params=%d entry=0x%x' % (i, d, tsz, params, entry))
