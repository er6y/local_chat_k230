import struct, json, re
SPEC = json.load(open('/root/isa_spec.json'))
OPC = SPEC['enums']['OPCODE']
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
mv = memoryview(data)
# 提取每区的 lui(2) 立即数（地址高 20 位）集合
def lui_set(i):
    off, tsz, params = kfuncs[i]
    p = off; end = off+tsz; vals=[]
    while p < end:
        w = int.from_bytes(mv[p:p+4],'little'); op = w & 0x7f
        if op == 2:  # lui: 提取 imm20（位 7-26 猜测，先按整个字高位看）
            vals.append((w >> 7) & 0xFFFFF)
        p += 2 if op in SET2 else 4
    return sorted(set(vals))
from collections import Counter
slow_addrs = Counter(); fast_addrs = Counter()
for i in range(n):
    for a in lui_set(i):
        mb = a >> 8  # 粗分桶（imm20 高 12 位 ≈ 4MB 桶）
        (slow_addrs if durs[i] > 2.0 else fast_addrs)[mb] += 1
print('SLOW regions lui-buckets:', sorted(slow_addrs.items())[:20])
print('FAST regions lui-buckets:', sorted(fast_addrs.items())[:20])
