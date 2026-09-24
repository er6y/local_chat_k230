import struct, sys

def parse(path, topn=8):
    data = open(path, "rb").read()
    print("== %s  %.1f MB" % (path.split("/")[-1], len(data)/1e6))
    ident, ver, flags, align, modules, emod, efun, rsv = struct.unpack_from("<8I", data, 0)
    pos = 32
    for m in range(modules):
        mstart = pos
        kind = data[pos:pos+16].rstrip(b"\x00").decode(errors="replace")
        mver, msec, mfun, mrsv, msize = struct.unpack_from("<IIIIQ", data, pos+16)
        pos += 40
        sizes = []
        for f in range(mfun):
            params, fsec, entry, textsz, fsize = struct.unpack_from("<IIQQQ", data, pos)
            sizes.append((textsz, f, params))
            pos += fsize if fsize >= 32 else 32
        if kind == "k230" and sizes:
            tot = sum(s[0] for s in sizes)
            top = sorted(sizes, reverse=True)[:topn]
            print("  total .text = %.2f MB" % (tot/1e6))
            for textsz, f, params in top:
                print("    fn%-5d text=%7.2fMB  (=%.1f%% of text)  params=%d" % (f, textsz/1e6, 100*textsz/tot, params))
        for s in range(msec):
            epos = pos
            pos = epos + struct.unpack_from("<I", data, pos+20)[0]
        pos = mstart + msize

for p in sys.argv[1:]:
    try:
        parse(p)
    except Exception as e:
        print("  ERR", repr(e))
    print()
