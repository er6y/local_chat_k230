import struct, json, re, sys
sys.path.insert(0, '/root')
SPEC = json.load(open('/root/isa_spec.json'))
OPC = SPEC['enums']['OPCODE']
INV = {v:k for k,v in OPC.items()}
SET2 = set()
for name, ins in SPEC['insts'].items():
    if ins.get('size') == 2 and 'fields' in ins:
        for f in ins['fields']:
            if f['kind'] == 'opcode': SET2.add(OPC[f['val']])

data = open('llm_kv6_q2cpu.kmodel','rb').read()
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

lines = open('prof_kv6q2cpu.txt', encoding='utf-8', errors='replace').read().splitlines()
starts = [i for i,l in enumerate(lines) if 'stack OPs timeline' in l]
seg = lines[starts[1]:starts[2]] if len(starts)>2 else lines[starts[1]:]
durs=[]
for ln in seg:
    m = re.match(r'\|EXTCALL\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|([0-9.eE+-]+)\s*\|', ln)
    if m: durs.append(float(m.group(3)))
n = min(len(durs), len(kfuncs))

# 找慢 conf 区(idx=25) 与一个快的同类型区（tsz 相近、dur<0.3）
slow_i = 25
fast_i = None
for i in range(n):
    off, tsz, params = kfuncs[i]
    if 250 <= tsz <= 400 and durs[i] < 0.3:
        fast_i = i; break
print('slow idx=%d dur=%.2f tsz=%d params=%d' % (slow_i, durs[slow_i], kfuncs[slow_i][1], kfuncs[slow_i][2]))
print('fast idx=%d dur=%.2f tsz=%d params=%d' % (fast_i, durs[fast_i], kfuncs[fast_i][1], kfuncs[fast_i][2]))
mv = memoryview(data)
def decode(i):
    off, tsz, params = kfuncs[i]
    p = off; end = off+tsz; out=[]
    while p < end:
        w = int.from_bytes(mv[p:p+4],'little'); op = w & 0x7f
        nm = OPC.get(op, 'op%d'%op)
        out.append((p-off, nm, w))
        p += 2 if op in SET2 else 4
    return out
for tag, i in [('SLOW', slow_i), ('FAST', fast_i)]:
    print('==== %s ====' % tag)
    for (o, nm, w) in decode(i):
        print('%4d %-22s %08x' % (o, nm, w))
