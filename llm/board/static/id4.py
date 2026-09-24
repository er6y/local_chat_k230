import struct, json, re
from collections import Counter
SPEC = json.load(open('/root/isa_spec.json'))
OPC = SPEC['enums']['OPCODE']
SET2 = set()
for name, ins in SPEC['insts'].items():
    if ins.get('size') == 2 and 'fields' in ins:
        for f in ins['fields']:
            if f['kind'] == 'opcode': SET2.add(OPC[f['val']])
data = open('llm_kv6_q2cpu.kmodel','rb').read()
N = len(data)
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
        if name.startswith('.text') and kind != 'stackvm': tbase = epos + 56 + sbs
        pos = epos + ssize
    if kind != 'stackvm':
        kfuncs = [(tbase + entry, tsz, params) for (params, entry, tsz) in recs]
    pos = mstart + msize
out = open('id4.out','w')
out.write('kfuncs=%d\n' % len(kfuncs))
lines = open('prof_kv6q2cpu.txt', encoding='utf-8', errors='replace').read().splitlines()
starts = [i for i,l in enumerate(lines) if 'stack OPs timeline' in l]
seg = lines[starts[1]:starts[2]] if len(starts)>2 else lines[starts[1]:]
durs=[]
for ln in seg:
    m = re.match(r'\|EXTCALL\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|', ln)
    if m: durs.append(float(m.group(3)))
out.write('durs=%d\n' % len(durs)); out.flush()
n = min(len(durs), len(kfuncs))
mv = memoryview(data)
def census(i, cap=8*1024*1024):
    off, tsz, params = kfuncs[i]
    p=off; end=min(off+tsz, off+cap); c=Counter()
    while p<end:
        w=int.from_bytes(mv[p:p+4],'little'); op=w&0x7f; c[op]+=1
        p += 2 if op in SET2 else 4
    return tsz, params, c
per = n/24.0
shown=0
for i in range(n):
    if durs[i] > 2.0:
        tsz, params, c = census(i)
        top = sorted(c.items(), key=lambda x:-x[1])[:7]
        out.write('SLOW idx=%d pos=%.1f dur=%.2f tsz=%d params=%d: %s\n' % (i, (i-4)%per, durs[i], tsz, params, ', '.join('%s=%d'%(OPC.get(o,o),v) for o,v in top)))
        shown+=1
        out.flush()
        if shown>=18: break
out.write('DONE\n'); out.close()
