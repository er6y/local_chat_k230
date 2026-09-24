import struct, json, re
from collections import Counter
SPEC = json.load(open('/root/isa_spec.json'))
OPC = SPEC['enums']['OPCODE']
SET2 = set()
for name, ins in SPEC['insts'].items():
    if ins.get('size') == 2 and 'fields' in ins:
        for f in ins['fields']:
            if f['kind'] == 'opcode': SET2.add(OPC[f['val']])
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
mv = memoryview(data)
def census(i, cap=1024):
    off, tsz, params = kfuncs[i]
    p = off
    end = min(off + tsz, off + cap)
    c = Counter()
    while p < end:
        w = int.from_bytes(mv[p:p+4], 'little')
        op = w & 0x7f
        c[op] += 1
        p += 2 if op in SET2 else 4
    return tsz, params, c
out = open('/mnt/data/static/id5.out', 'w')
out.write('nfuncs=%d ndurs=%d\n' % (len(kfuncs), len(durs)))
tiny_slow = [i for i in range(n) if durs[i] >= 2.0 and kfuncs[i][1] < 1000]
tiny_fast = [i for i in range(n) if durs[i] < 0.1 and 100 < kfuncs[i][1] < 1000]
out.write('tiny_slow=%d tiny_fast=%d\n' % (len(tiny_slow), len(tiny_fast)))
cs = Counter()
for i in tiny_fast[:30]:
    tsz, prm, c = census(i)
    cs += c
out.write('FAST tiny agg top: %s\n' % ', '.join('%s=%d' % (OPC.get(o, o), v) for o, v in sorted(cs.items(), key=lambda x: -x[1])[:12]))
out.write('SLOW tiny tsz set: %s\n' % sorted(set(kfuncs[i][1] for i in tiny_slow)))
out.write('FAST tiny tsz set: %s\n' % sorted(set(kfuncs[i][1] for i in tiny_fast))[:30])
mid_fast = [i for i in range(n) if durs[i] < 0.1 and 10000 <= kfuncs[i][1] <= 200000]
mid_slow = [i for i in range(n) if durs[i] >= 2.0 and 10000 <= kfuncs[i][1] <= 200000]
out.write('mid_fast=%d mid_slow=%d\n' % (len(mid_fast), len(mid_slow)))
out.write('mid_fast tsz: %s\n' % sorted(set(kfuncs[i][1] for i in mid_fast))[:20])
out.write('mid_slow tsz: %s\n' % sorted(set(kfuncs[i][1] for i in mid_slow))[:20])
for lbl, lst in [('MID_FAST', mid_fast[:2]), ('MID_SLOW', mid_slow[:2])]:
    for i in lst:
        tsz, prm, c = census(i, cap=200000)
        top = sorted(c.items(), key=lambda x: -x[1])[:10]
        out.write('%s idx=%d dur=%.2f tsz=%d: %s\n' % (lbl, i, durs[i], tsz, ', '.join('%s=%d' % (OPC.get(o, o), v) for o, v in top)))
out.write('DONE\n')
out.close()
print('ID5_DONE')
