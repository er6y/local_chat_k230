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

fns, toff = walk('/mnt/data/qwen_official/ERNIE128/llm.kmodel')
params, entry, tsz = fns[1]
f = open('/mnt/data/qwen_official/ERNIE128/llm.kmodel', 'rb')
f.seek(toff + entry)
blob = f.read(min(tsz, 2 * 1024 * 1024))
f.close()
print('OURS fn1 text=%.2fMB sample=%.1fKB' % (tsz / 1e6, len(blob) / 1024))

# period detection: find smallest P in [8..4096] (4-byte units) where blob[0:P] repeats at P, 2P, 3P
base = blob[:4096]
best = None
for P in range(8, 4097, 4):
    ok = True
    for k in range(1, 6):
        if blob[k * P:k * P + 32] != base[:32]:
            ok = False
            break
    if ok:
        best = P
        break
print('repeating unit (bytes) =', best * 4 if best else None)
if best:
    P = best * 4
    print('exact repeats in full fn:', tsz / P)
    # compare unit with official fn0 head
    fo = open('/mnt/data/qwen_official/Qwen2.5-0.5B-Instruct/llm.kmodel', 'rb')
    ofns, otoff = walk('/mnt/data/qwen_official/Qwen2.5-0.5B-Instruct/llm.kmodel')
    oparams, oentry, osz = ofns[0]
    fo.seek(otoff + oentry)
    oblob = fo.read(min(osz, 4096))
    fo.close()
    print('official fn0 head:', oblob[:64].hex())
    print('ours unit head   :', blob[:64].hex())
    print('ours unit tail   :', blob[P - 32:P].hex())
