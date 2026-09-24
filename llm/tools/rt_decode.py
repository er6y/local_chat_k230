import json, re, struct, sys, hashlib
from collections import Counter

SPEC = json.load(open('/root/isa_spec.json'))
ENUMS = SPEC['enums']
INSTS = SPEC['insts']
OPC = ENUMS['OPCODE']

# 派发表: opcode -> [(name, size, [(pos, width, expect_val, is_const, field_type, field_name)])]
def build_table():
    tbl = {}
    for name, ins in INSTS.items():
        if 'fields' not in ins:
            continue
        pos = 0
        opc = None
        consts = []
        fields = []
        for f in ins['fields']:
            if f['kind'] == 'opcode':
                opc = OPC[f['val']]
            elif f['kind'] == 'const':
                v = int(f['val'])
                consts.append((pos, f['bits'], v))
            fields.append((pos, f['bits'], f))
            pos += f['bits']
        if opc is None:
            continue
        tbl.setdefault(opc, []).append((name, ins['size'], consts, fields))
    return tbl

TBL = build_table()
RV_FUNCT = {
    'branch': ('funct3', 'BRANCH_FUNCTION'),
    'arithm': ('funct5', 'ARITHMETIC_FUNCTION'),
    'arithm_imm': ('funct5', 'ARITHMETIC_IMM_FUNCTION'),
    'load': ('funct3', 'LOAD_FUNCTION'),
    'store': ('funct3', 'STORE_FUNCTION'),
}

def getbits(word, pos, width):
    return (word >> pos) & ((1 << width) - 1)

def decode_word(word):
    opc = word & 0x7F
    cands = TBL.get(opc)
    if not cands:
        return None
    # 先试 2 字节候选（无歧义时优先短指令）
    for name, size, consts, fields in cands:
        if size != (2 if opc <= word.bit_length() else 4):
            pass
    # 按常量字段匹配
    best = None
    for name, size, consts, fields in cands:
        ok = True
        for pos, width, val in consts:
            if getbits(word, pos, width) != val:
                ok = False
                break
        if not ok:
            continue
        # RV 基础族用 funct 派生名字
        if name in ('ARITHMETIC',):
            pass
        best = (name, size, fields)
        if size == 2:
            return best
    return best

def decode_bytes(data, limit=0):
    out = []
    i = 0
    unk = 0
    n = len(data)
    while i < n:
        w16 = data[i] | (data[i+1] << 8) if i + 1 < n else data[i]
        r = decode_word(w16)
        if r is not None and r[1] == 2:
            name, size, fields = r
        else:
            if i + 4 > n:
                break
            w32 = int.from_bytes(data[i:i+4], 'little')
            r = decode_word(w32)
            if r is None:
                unk += 1
                out.append(('UNK_%02x' % (w32 & 0x7F), 4, w32, i, []))
                i += 4
                continue
            name, size, fields = r
            if size != 4:
                unk += 1
                out.append(('SIZE_CONFLICT_%s' % name, 4, w32, i, []))
                i += 4
                continue
            w = w32
        if r[1] == 2:
            name, size, fields = r
            w = w16
        else:
            w = w32
        args = []
        for pos, width, f in fields:
            v = getbits(w, pos, width)
            if f['kind'] == 'opcode':
                continue
            args.append((f['val'], v, f['type']))
        out.append((name, size, w, i, args))
        i += size
        if limit and len(out) >= limit:
            break
    return out, unk

def fmt(name, w, args):
    if name.startswith('UNK') or name.startswith('SIZE_'):
        return '%s(0x%08x)' % (name, w)
    parts = []
    for fn, v, t in args:
        e = ENUMS.get(t, {})
        if fn in ('rd', 'rs', 'rs1', 'rs2', 'raddr_d', 'raddr_s', 'raddr_bw', 'rvalid_c_num', 'rgroups', 'rtarget', 'rd_basement', 'rd_offset', 'rd_target') and t == 'GP_REGISTER':
            parts.append('%s=x%d' % (fn, v))
        elif t == 'SHAPE_REGISTER':
            parts.append('%s=ss%d' % (fn, v))
        elif e:
            inv = {vv: kk for kk, vv in e.items()}
            parts.append('%s=%s' % (fn, inv.get(v, str(v))))
        else:
            parts.append('%s=%d' % (fn, v))
    return 'I.%s(%s)' % (name, ', '.join(parts))

# ---- main ----
def parse_ledger(path):
    f = open(path, 'rb')
    hdr = f.read(32)
    ident, ver, flags, align, modules, emod, efun, rsv = struct.unpack('<8I', hdr)
    pos = 32
    res = {}
    for m in range(modules):
        mstart = pos
        f.seek(pos); mh = f.read(56)
        kind = mh[:16].rstrip(b'\x00').decode()
        mver, msec, mfun, mrsv, msize = struct.unpack_from('<IIIIQ', mh, 16)
        pos += 40
        funcs = []
        for fi in range(mfun):
            f.seek(pos); rec = f.read(32)
            params, fsec, entry, textsz, fsize = struct.unpack_from('<IIQQQ', rec)
            funcs.append((fi, entry, textsz))
            pos += fsize if fsize >= 32 else 32
        secs = {}
        for s in range(msec):
            epos = pos
            f.seek(pos); sh = f.read(56)
            nm = sh[:16].rstrip(b'\x00').decode()
            sflags, srsv, ssize, sbs, sbsz, smem = struct.unpack_from('<IIQQQQ', sh, 16)
            secs[nm] = (epos + 56 + sbs, sbsz)
            pos = epos + ssize
        if kind == 'k230':
            res['funcs'] = funcs
            res['base'] = secs['.text'][0]
        pos = mstart + msize
    f.close()
    return res

if __name__ == '__main__':
    path, fidx = sys.argv[1], int(sys.argv[2])
    led = parse_ledger(path)
    fi, entry, textsz = [t for t in led['funcs'] if t[0] == fidx][0]
    f = open(path, 'rb')
    f.seek(led['base'] + entry)
    data = f.read(textsz)
    insts, unk = decode_bytes(data)
    print('fn%d: %d bytes -> %d instructions, %d unknown' % (fidx, textsz, len(insts), unk))
    cnt = Counter(name for name, s, w, i, a in insts)
    print('top instructions:')
    for nm, c in cnt.most_common(18):
        print('  %-24s %d' % (nm, c))
    # 每 PU_COMPUTE 前后的指令模式（取前 3 个 compute 的上下文）
    idxs = [k for k, (name, s, w, i, a) in enumerate(insts) if name == 'PU_COMPUTE'][:2]
    for k in idxs:
        print('--- context before PU_COMPUTE #%d ---' % k)
        for j in range(max(0, k-14), k+2):
            nm, s, w, i, a = insts[j]
            print('  @%06d %s' % (i, fmt(nm, w, a)))
    open('/root/k230re/fn%d decoded.txt' % fidx, 'w').write('\n'.join(fmt(nm, w, a) for nm, s, w, i, a in insts))
    print('full decode written')
