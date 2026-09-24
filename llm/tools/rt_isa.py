import json, re, struct, sys, hashlib

SPEC = json.load(open('/root/k230re/isa_spec.json'))
ENUMS = SPEC['enums']
INSTS = SPEC['insts']

def enc_val(f, arg, out_bits):
    kind = f['kind']
    if kind == 'opcode' or kind == 'const':
        v = ENUMS.get(f['type'], {}).get(f['val'], None) if kind == 'opcode' else int(f['val'])
        if kind == 'opcode':
            v = ENUMS['OPCODE'][f['val']]
        return v & ((1 << out_bits) - 1)
    if kind == 'enum' or kind == 'raw':
        s = arg.strip()
        if s.startswith('x'):
            return int(s[1:]) & ((1 << out_bits) - 1)
        if s.startswith('ss'):
            return int(s[2:]) & ((1 << out_bits) - 1)
        if re.match(r'^-?\d+$', s):
            return int(s) & ((1 << out_bits) - 1)
        e = ENUMS.get(f['type'], {})
        if s in e:
            return e[s] & ((1 << out_bits) - 1)
        for ev in ENUMS.values():
            if s in ev:
                return ev[s] & ((1 << out_bits) - 1)
        raise KeyError('enum value %s (type %s) not found' % (s, f['type']))
    if kind == 'imm':
        if arg in ('False', 'True'):
            return int(arg == 'True')
        return int(arg, 0) & ((1 << out_bits) - 1)
    raise ValueError('kind ' + kind)

def enc_simple(name, args):
    inst = INSTS[name]
    nbits = sum(f['bits'] for f in inst['fields'])
    assert nbits == inst['size'] * 8, (name, nbits, inst['size'])
    val = 0
    pos = 0
    for f in inst['fields']:
        arg = args.get(f['val'], args.get(f['val'].lower(), '0'))
        v = enc_val(f, arg, f['bits'])
        val |= v << pos
        pos += f['bits']
    return val.to_bytes(inst['size'], 'little')

def enc_loadimm(rd, v):
    v = int(v, 0)
    r = rd if rd.startswith('x') else 'x' + rd
    out = b''
    if v < 2048 and v != 0:
        out += enc_simple('ADDI', {'rd': r, 'rs': 'x0', 'funct5': 'addi', 'imm': str(v)})
    else:
        hi = ((v >> 12) + ((v >> 11) & 1)) & 0xFFFFF
        lo = v & 0xFFF
        out += enc_simple('LUI', {'rd': r, 'imm': str(hi)})
        out += enc_simple('ADDI', {'rd': r, 'rs': r, 'funct5': 'addi', 'imm': str(lo)})
    return out

def enc_ddraddr(args):
    b = args['rd_basement']; B = int(args['basement'], 0)
    t = args['rd_target']; O = int(args['offset'], 0)
    out = enc_simple('LW', {'rd': b, 'rs': 'x0', 'funct3': 'lw', 'offset': str(B * 4)})
    out += enc_loadimm(t, str(O))
    out += enc_simple('ADD', {'rd': t, 'rs1': b, 'funct5': 'add', 'rs2': t})
    return out

def encode_line(line):
    m = re.match(r'\s*I\.(\w+)\((.*)\)\s*$', line)
    if not m:
        return None
    name, argstr = m.group(1), m.group(2)
    args = {}
    if argstr.strip():
        for part in split_top(argstr):
            k, v = part.split(':', 1)
            args[k.strip()] = v.strip()
    if name == 'LoadImm':
        return enc_loadimm(args['rd'], args['value'])
    if name in ('LoadDDrAddr', 'LoadDdrAddr'):
        m2 = re.search(r'rd_basement:\s*(x\d+),\s*value:\s*(\d+);\s*rd_offset:\s*(x\d+),\s*value:\s*(\d+)', argstr)
        return enc_ddraddr({'rd_basement': m2.group(1), 'basement': m2.group(2), 'rd_target': m2.group(3), 'offset': m2.group(4)})
    if name not in INSTS or 'fields' not in INSTS[name]:
        raise KeyError('no spec for ' + name)
    return enc_simple(name, args)

def split_top(s):
    parts, depth, cur = [], 0, ''
    for ch in s:
        if ch == '(':
            depth += 1
        elif ch == ')':
            depth -= 1
        if ch == ',' and depth == 0:
            parts.append(cur.strip()); cur = ''
        else:
            cur += ch
    if cur.strip():
        parts.append(cur.strip())
    return parts

# ---- main: round-trip mini ----
def parse_ledger_funcs(path):
    f = open(path, 'rb')
    hdr = f.read(32)
    ident, ver, flags, align, modules, emod, efun, rsv = struct.unpack('<8I', hdr)
    pos = 32
    out = {}
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
            name2 = sh[:16].rstrip(b'\x00').decode()
            sflags, srsv, ssize, sbs, sbsz, smem = struct.unpack_from('<IIQQQQ', sh, 16)
            secs[name2] = (epos + 56 + sbs, sbsz)
            pos = epos + ssize
        if kind == 'k230':
            out['funcs'] = funcs; out['text_base'] = secs['.text'][0]
        pos = mstart + msize
    f.close()
    return out

kmod, scriptpath = sys.argv[1], sys.argv[2]
led = parse_ledger_funcs(kmod)
base = led['text_base']
tb = open(kmod, 'rb')
lines = open(scriptpath).read().splitlines()
enc = b''
bounds = []
n = 0
for ln in lines:
    if not ln.strip().startswith('I.'):
        continue
    b = encode_line(ln)
    assert b, ln
    bounds.append((len(enc), ln.strip(), b))
    enc += b
    n += 1
print('encoded %d instructions -> %d bytes' % (n, len(enc)))
# 找匹配的函数（按尺寸）
for fi, entry, textsz in led['funcs']:
    if textsz == len(enc):
        tb.seek(base + entry); real = tb.read(textsz)
        if real == enc:
            print('fn%d MATCH md5=%s' % (fi, hashlib.md5(real).hexdigest()))
        else:
            diff = next(i for i in range(min(len(real), len(enc))) if real[i] != enc[i])
            print('fn%d MISMATCH at byte %d' % (fi, diff))
            for off, ln, b in bounds:
                if off <= diff < off + len(b):
                    st = max(0, diff - off - 6)
                    print(' INSTR @%d: %s' % (off, ln[:120]))
                    print('  real', real[off:off+len(b)].hex())
                    print('  enc ', b.hex())
                    break
        break
else:
    print('no function with size %d (avail: %s)' % (len(enc), [(fi, t) for fi, e, t in led['funcs']][:6]))
