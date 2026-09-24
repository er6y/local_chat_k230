import struct
import sys

pid = int(sys.argv[1])
maps = open('/proc/%d/maps' % pid).read().splitlines()
pm = open('/proc/%d/pagemap' % pid, 'rb')
PAGESIZE = 4096


def vaddr_pfn(vaddr):
    idx = vaddr // PAGESIZE
    pm.seek(idx * 8)
    e = pm.read(8)
    if len(e) < 8:
        return None
    v = struct.unpack('<Q', e)[0]
    if v & (1 << 63):
        return v & 0x7FFFFFFFFFFFFF
    return None


# find big anonymous or file mappings
regions = []
for ln in maps:
    parts = ln.split()
    rng = parts[0]
    a, b = rng.split('-')
    a, b = int(a, 16), int(b, 16)
    sz = b - a
    if sz >= 8 * 1024 * 1024:
        regions.append((a, b, sz, ln))
print('big mappings:')
for a, b, sz, ln in regions:
    print('  %08x-%08x %6.1fMB %s' % (a, b, sz / 1e6, ln[ln.find(']')+1:].strip() if ']' in ln else ' '.join(parts[1:])))
# probe phys pages across each big region: sample every 1/16
print('phys sampling (present pages):')
for a, b, sz, ln in regions:
    samples = []
    for k in range(17):
        va = a + (sz - PAGESIZE) * k // 16
        pfn = vaddr_pfn(va)
        if pfn:
            samples.append((k // 16, va, pfn * 4096))
    for frac, va, pa in samples[:18]:
        print('  %08x -> phys %08x  frac=%2d/16' % (va, pa, frac))
