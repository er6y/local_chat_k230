import struct, sys
from collections import Counter
data = open(sys.argv[1],'rb').read()
ident, ver, flags, align, modules, emod, efun, rsv = struct.unpack_from('<8I', data, 0)
pos = 32
for m in range(modules):
    mstart = pos
    kind = data[pos:pos+16].rstrip(b'\x00').decode(errors='replace')
    mver, msec, mfun, mrsv, msize = struct.unpack_from('<IIIIQ', data, pos+16)
    pos += 40
    recs = []
    for f in range(mfun):
        params, fsec, entry, textsz, fsize = struct.unpack_from('<IIQQQ', data, pos)
        recs.append(textsz)
        pos += fsize if fsize >= 32 else 32
    if kind != 'stackvm':
        print('mod %s: functions=%d' % (kind, mfun))
        c = Counter()
        for tsz in recs:
            if tsz >= 1000000: c['>=1MB'] += 1
            elif tsz >= 100000: c['100K-1M'] += 1
            elif tsz >= 10000: c['10K-100K'] += 1
            elif tsz >= 1000: c['1K-10K'] += 1
            else: c['<1K'] += 1
        for k in ['<1K','1K-10K','10K-100K','100K-1M','>=1MB']:
            print('  tsz %-9s %d' % (k, c[k]))
        big = sorted(recs)[-6:]
        print('  top6 tsz:', big)
    pos = mstart + msize
