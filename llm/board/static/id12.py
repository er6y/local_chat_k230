import sys

pid = int(sys.argv[1])
rows = []
for ln in open('/proc/%d/maps' % pid):
    if '/dev/mmz' not in ln:
        continue
    parts = ln.split()
    a, b = parts[0].split('-')
    sz = int(b, 16) - int(a, 16)
    phys = parts[2]
    if sz >= 4096:
        rows.append((sz, phys, int(phys, 16), int(a, 16), int(b, 16)))
rows.sort(reverse=True)
print('mmz mappings (size desc):')
for sz, phys, p, va, vb in rows[:25]:
    flag = ' <== crosses 1GB' if p < 0x40000000 and p + sz > 0x40000000 else ''
    flag += ' <== above 2GB' if p >= 0x80000000 else ''
    print('  phys=0x%08x size=%8.2fMB end=0x%08x%s' % (p, sz / 1048576, p + sz, flag))
tot = sum(r[0] for r in rows)
print('total mmz mapped: %.1fMB in %d mappings' % (tot / 1048576, len(rows)))
