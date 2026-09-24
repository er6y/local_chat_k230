import struct

def walk(path):
    f = open(path, 'rb')
    f.seek(0)
    hdr = f.read(32)
    ident, ver, flags, align, modules, emod, efun, rsv = struct.unpack('<8I', hdr)
    pos = 32
    for m in range(modules):
        mstart = pos
        f.seek(pos)
        mh = f.read(56)
        kind = mh[:16].rstrip(b'\x00').decode()
        mver, msec, mfun, mrsv, msize = struct.unpack_from('<IIIIQ', mh, 16)
        pos += 40
        fns = []
        for fi in range(mfun):
            f.seek(pos)
            rec = f.read(32)
            params, fsec, entry, textsz, fsize = struct.unpack_from('<IIQQQ', rec)
            fns.append((params, entry, textsz))
            pos += fsize if fsize >= 32 else 32
        text_off = None
        for s in range(msec):
            epos = pos
            f.seek(pos)
            sh = f.read(56)
            name = sh[:16].rstrip(b'\x00').decode()
            sflags, srsv, ssize, sbs, sbsz, smem = struct.unpack_from('<IIQQQQ', sh, 16)
            if kind == 'k230' and name == '.text':
                text_off = epos + 56 + sbs
            pos = epos + ssize
        if kind == 'k230' and text_off is not None:
            f.close()
            return fns, text_off
        pos = mstart + msize
    f.close()
    return None, None

def stats(path, fnidx_list, tag):
    fns, toff = walk(path)
    f = open(path, 'rb')
    for fi in fnidx_list:
        params, entry, tsz = fns[fi]
        f.seek(toff + entry)
        blob = f.read(min(tsz, 262144))
        n = len(blob) // 4 * 4
        words = struct.unpack('<%dI' % (n // 4), blob[:n])
        hist = {}
        for w in words:
            hist[w] = hist.get(w, 0) + 1
        top = sorted(hist.items(), key=lambda x: -x[1])[:6]
        tot = sum(hist.values())
        uniq = len(hist)
        print('%s fn%d params=%d text=%.2fMB' % (tag, fi, params, tsz / 1e6))
        print('   first32=%s' % blob[:32].hex())
        print('   uniq words=%d of %d; top:' % (uniq, tot))
        for w, c in top:
            print('     %08x  %6d  %.2f%%' % (w, c, 100.0 * c / tot))
        for B in (32, 64, 128):
            same = sum(1 for i in range(0, 8192 - B, B) if blob[i:i + 4] == blob[i + B:i + B + 4])
            print('   bundle=%d head-word repeat %d/%d' % (B, same, 8192 // B))
    f.close()

stats('/mnt/data/qwen_official/Qwen2.5-0.5B-Instruct/llm.kmodel', [0, 2], 'OFFICIAL')
stats('/mnt/data/qwen_official/ERNIE128/llm.kmodel', [1, 3], 'OURS')
