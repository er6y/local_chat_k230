import struct

f = open('D:/work/git_dev/k230_prj/tmp/nncase_rt.so', 'rb')
data = f.read()
assert data[:4] == b'\x7fELF'
is64 = data[4] == 2
ei_data = data[5]
end = '<' if ei_data == 1 else '>'
e_type, e_machine = struct.unpack_from(end + 'HH', data, 16)
e_shoff, = struct.unpack_from(end + 'Q', data, 0x28)
e_shentsize, e_shnum, e_shstrndx = struct.unpack_from(end + 'HHH', data, 0x3A)
print('type=%d machine=%d(243=riscv) sections=%d' % (e_type, e_machine, e_shnum))
secs = []
for i in range(e_shnum):
    off = e_shoff + i * e_shentsize
    name, stype, flags, addr, offset, size, link, info, align, entsize = struct.unpack_from(end + 'IIQQQQIIQQ', data, off)
    secs.append(dict(name=name, stype=stype, addr=addr, offset=offset, size=size, link=link, entsize=entsize))
shstr = secs[e_shstrndx]


def sname(s):
    p = shstr['offset'] + s['name']
    e = data.index(b'\x00', p)
    return data[p:e].decode()


for s in secs:
    s['n'] = sname(s)
symtab = next(s for s in secs if s['n'] == '.dynsym')
strtab = next(s for s in secs if s['n'] == '.dynstr')


def sym_name(idx):
    p = strtab['offset'] + idx
    e = data.index(b'\x00', p)
    return data[p:e].decode()


syms = []
n = symtab['size'] // 24
for i in range(n):
    off = symtab['offset'] + i * 24
    nm, info, other, shndx, value, size_ = struct.unpack_from(end + 'IBBHQQ', data, off)
    syms.append((nm, value, size_, info))


def find(name):
    out = []
    for i, (nm, v, sz, info) in enumerate(syms):
        if name in sym_name(nm):
            out.append((i, sym_name(nm), hex(v), sz, info & 1 == 1))
    return out


for q in ('mmz_flush', 'mmz_alloc', 'mmz_init', 'mmz_free'):
    print(q, find(q))

# find relocation sections referencing dynsym
for s in secs:
    if s['stype'] in (4, 7) and s['link'] != 0:  # RELA
        tag = 'REL'
        cnt = s['size'] // 24
        hits = 0
        for i in range(cnt):
            off = s['offset'] + i * 24
            r_offset, r_info, r_addend = struct.unpack_from(end + 'QQq', data, off)
            symidx = r_info >> 32
            if symidx >= len(syms):
                continue
            name = sym_name(syms[symidx][0])
            if 'mmz_flush' in name:
                print('RELA in %s: offset=0x%x sym=%s type=%d' % (s['n'], r_offset, name, r_info & 0xFFFFFFFF))
                hits += 1
                if hits > 10:
                    break
