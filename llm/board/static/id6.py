import struct, json, re
from collections import Counter, defaultdict
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
# group call sites by entry address (same function object)
by_entry = defaultdict(list)
for i in range(n):
    by_entry[kfuncs[i][0]].append((i, durs[i], kfuncs[i][1]))
multi = {e: v for e, v in by_entry.items() if len(v) >= 2}
print('call_sites=%d unique_entries=%d multi_site_entries=%d' % (n, len(by_entry), len(multi)))
mixed = 0
for e, v in sorted(multi.items(), key=lambda x: -len(x[1]))[:15]:
    durs_sorted = sorted(d for i, d, t in v)
    tsz = v[0][2]
    tag = ''
    if max(d for i, d, t in v) >= 2.0 and min(d for i, d, t in v) < 0.5:
        tag = ' <== MIXED'; mixed += 1
    print('entry=0x%x tsz=%d sites=%d durs[min/med/max]=%.3f/%.3f/%.3f%s' % (
        e, tsz, len(v), durs_sorted[0], durs_sorted[len(durs_sorted)//2], durs_sorted[-1], tag))
print('MIXED entries:', mixed)
