import struct, sys

def parse(path):
    data = open(path, "rb").read()
    print("== %s  %.1f MB" % (path.split("/")[-1], len(data)/1e6))
    ident, ver, flags, align, modules, emod, efun, rsv = struct.unpack_from("<8I", data, 0)
    pos = 32
    for m in range(modules):
        mstart = pos
        kind = data[pos:pos+16].rstrip(b"\x00").decode(errors="replace")
        mver, msec, mfun, mrsv, msize = struct.unpack_from("<IIIIQ", data, pos+16)
        pos += 40
        for f in range(mfun):
            params, fsec, entry, textsz, fsize = struct.unpack_from("<IIQQQ", data, pos)
            pos += fsize if fsize >= 32 else 32
        for s in range(msec):
            epos = pos
            name = data[pos:pos+16].rstrip(b"\x00").decode(errors="replace")
            sflags, srsv, ssize, sbs, sbsz, smem = struct.unpack_from("<IIQQQQ", data, pos+16)
            print("  mod%d(%-7s) %-8s body=%8.1fMB mem=%8.1fMB (body/mem=%.2f)" % (m, kind, name, sbsz/1e6, smem/1e6, sbsz/smem if smem else 0))
            pos = epos + ssize
        pos = mstart + msize

for p in sys.argv[1:]:
    try:
        parse(p)
    except Exception as e:
        print("  ERR", repr(e))
    print()
