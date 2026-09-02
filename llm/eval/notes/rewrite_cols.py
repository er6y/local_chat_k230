import io

p = r'D:\work\git_dev\k230_prj\k230_llm\llamacpp\ggml\src\ggml-cpu\arch\riscv\quants.c'
src = io.open(p, encoding='utf-8').read()

start_marker = "    if (ncol == 16) {\n"
end_marker = "        for (int c = 0; c < 16; ++c) dst[c] = acc[c];\n        return;\n    }\n"

i0 = src.index(start_marker)
i1 = src.index(end_marker, i0)

group = r"""            "vsetvli zero, t6, e8, m2, ta, ma\n\t"
            "vle8.v v4, 0(@p0@)\n\t"
            "vwmul.vv v16, v4, v2\n\t"
            "vle8.v v4, 0(@p1@)\n\t"
            "vwmul.vv v20, v4, v2\n\t"
            "vle8.v v4, 0(@p2@)\n\t"
            "vwmul.vv v24, v4, v2\n\t"
            "vle8.v v4, 0(@p3@)\n\t"
            "vwmul.vv v28, v4, v2\n\t"
            "vsetvli zero, t6, e16, m4, ta, ma\n\t"
            "vwredsum.vs v8, v16, v0\n\t"
            "vwredsum.vs v9, v20, v0\n\t"
            "vwredsum.vs v10, v24, v0\n\t"
            "vwredsum.vs v11, v28, v0\n\t"
            "vsetvli zero, t6, e32, m1, ta, ma\n\t"
            "vmv.x.s a0, v8\n\t"
            "fcvt.s.w f4, a0\n\t"
            "flh f5, -2(@p0@)\n\t"
            "fcvt.s.h f5, f5\n\t"
            "fmul.s f5, f5, f0\n\t"
            "fmadd.s %[c@c0@], f4, f5, %[c@c0@]\n\t"
            "vmv.x.s a1, v9\n\t"
            "fcvt.s.w f6, a1\n\t"
            "flh f7, -2(@p1@)\n\t"
            "fcvt.s.h f7, f7\n\t"
            "fmul.s f7, f7, f0\n\t"
            "fmadd.s %[c@c1@], f6, f7, %[c@c1@]\n\t"
            "vmv.x.s a2, v10\n\t"
            "fcvt.s.w f8, a2\n\t"
            "flh f9, -2(@p2@)\n\t"
            "fcvt.s.h f9, f9\n\t"
            "fmul.s f9, f9, f0\n\t"
            "fmadd.s %[c@c2@], f8, f9, %[c@c2@]\n\t"
            "vmv.x.s a3, v11\n\t"
            "fcvt.s.w f10, a3\n\t"
            "flh f11, -2(@p3@)\n\t"
            "fcvt.s.h f11, f11\n\t"
            "fmul.s f11, f11, f0\n\t"
            "fmadd.s %[c@c3@], f10, f11, %[c@c3@]\n\t"
"""

advance = r"""            "addi s0, s0, 34\n\t"
            "addi s1, s1, 34\n\t"
            "addi s2, s2, 34\n\t"
            "addi s3, s3, 34\n\t"
            "addi s4, s4, 34\n\t"
            "addi s5, s5, 34\n\t"
            "addi s6, s6, 34\n\t"
            "addi s7, s7, 34\n\t"
            "addi t4, t4, 34\n\t"
            "addi t5, t5, -1\n\t"
            "bnez t5, 1b\n\t"
"""

def fill_group(p0, p1, p2, p3, c0, c1, c2, c3):
    g = group
    for a, b in (("@p0@", p0), ("@p1@", p1), ("@p2@", p2), ("@p3@", p3),
                 ("@c0@", str(c0)), ("@c1@", str(c1)),
                 ("@c2@", str(c2)), ("@c3@", str(c3))):
        g = g.replace(a, b)
    return g

def make_asm(vx_expr, cbase):
    setup = r"""            "li t6, 32\n\t"
            "vsetvli zero, t6, e32, m1, ta, ma\n\t"
            "vmv.v.i v0, 0\n\t"
            "mv s0, %[vx]\n\t"
            "addi s0, s0, 2\n\t"
            "add s1, s0, %[bx]\n\t"
            "add s2, s1, %[bx]\n\t"
            "add s3, s2, %[bx]\n\t"
            "add s4, s3, %[bx]\n\t"
            "add s5, s4, %[bx]\n\t"
            "add s6, s5, %[bx]\n\t"
            "add s7, s6, %[bx]\n\t"
            "mv t4, %[vy]\n\t"
            "addi t4, t4, 2\n\t"
            "mv t5, %[cnt]\n\t"
            "1:\n\t"
            "vsetvli zero, t6, e8, m2, ta, ma\n\t"
            "vle8.v v2, 0(t4)\n\t"
            "flh f0, -2(t4)\n\t"
            "fcvt.s.h f0, f0\n\t"
"""
    body = fill_group("s0", "s1", "s2", "s3", cbase + 0, cbase + 1, cbase + 2, cbase + 3)
    body += fill_group("s4", "s5", "s6", "s7", cbase + 4, cbase + 5, cbase + 6, cbase + 7)
    ops = "".join('[c%d]"+f"(acc[%d]), ' % (i, i) for i in range(cbase, cbase + 8))
    ops = ops.rstrip(', ')
    return ('        __asm__ __volatile__(\n' + setup + body + advance +
            '            : ' + ops +
            '\n            : [vx]"r"(' + vx_expr + '), [bx]"r"(bx),\n'
            '              [vy]"r"((const char *)vy), [cnt]"r"(nb)\n'
            '            : "t0","t1","t2","t3","t4","t5","t6","a0","a1","a2","a3",\n'
            '              "s0","s1","s2","s3","s4","s5","s6","s7",\n'
            '              "f0","f4","f5","f6","f7","f8","f9","f10","f11",\n'
            '              "v0","v2","v4","v8","v9","v10","v11",\n'
            '              "v16","v17","v18","v19","v20","v21","v22","v23",\n'
            '              "v24","v25","v26","v27","v28","v29","v30","v31",\n'
            '              "memory","cc");\n')

new_block = (
    "    if (ncol == 16) {\n"
    "        // 16 columns as two 8-col asm passes: 12 operands each\n"
    "        // (this GCC double-counts \"+f\": 16 accs + 4 in trips the\n"
    "        // 30-operand asm limit). 2 y loads/block + 12 vsetvli/block,\n"
    "        // vs 16 + 48 for per-column calls.\n"
)
new_block += make_asm("(const char *)vx", 0)
new_block += make_asm("(const char *)vx + 8 * bx", 8)
new_block += (
    "\n"
    "        for (int c = 0; c < 16; ++c) dst[c] = acc[c];\n"
    "        return;\n"
    "    }\n"
)

out = src[:i0] + new_block + src[i1 + len(end_marker):]
io.open(p, 'w', encoding='utf-8', newline='\n').write(out)
print("replaced ok, new block %d bytes" % len(new_block))
