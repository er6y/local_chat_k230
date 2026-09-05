#!/usr/bin/env python3
# bisect_compile.py — find the smallest prefix of the C128 graph that crashes
# the nncase compile (f32, no PTQ). Exposes prefix outputs and compiles.
import subprocess
import sys
import numpy as np
import onnx

PC = '/tmp/matcha-icefall-zh-baker/matcha_C128.onnx'
m = onnx.load(PC)
nodes = list(m.graph.node)
N = len(nodes)
print('C128 nodes:', N)


def build_prefix(k, path):
    mm = onnx.load(PC)
    gg = mm.graph
    keep = nodes[:k]
    kept_outs = {o for n in keep for o in n.output}
    ins = {i.name for i in gg.input}
    init_names = {i.name for i in gg.initializer}
    prod_by = {o: n for n in keep for o in n.output}
    # output = last node's output
    last = keep[-1]
    del gg.node[:]
    del gg.output[:]
    gg.node.extend(keep)
    for o in last.output:
        gg.output.append(onnx.helper.make_tensor_value_info(o, onnx.TensorProto.FLOAT, None))
    # drop unused initializers
    used = {i for n in keep for i in n.input}
    inits = [i for i in gg.initializer if i.name in used]
    del gg.initializer[:]
    gg.initializer.extend(inits)
    # drop unused inputs
    real_ins = [i for i in gg.input if i.name in used]
    del gg.input[:]
    gg.input.extend(real_ins)
    onnx.save(mm, path)


def try_compile(path):
    code = f'''
import nncase
co = nncase.CompileOptions()
co.target = "k230"
co.dump_ir = False
co.dump_asm = False
compiler = nncase.Compiler(co)
with open("{path}", "rb") as f:
    compiler.import_onnx(f.read(), nncase.ImportOptions())
compiler.compile()
print("OK")
'''
    r = subprocess.run([sys.executable, '-c', code], capture_output=True, text=True,
                       timeout=600)
    ok = 'OK' in r.stdout and r.returncode == 0
    return ok


lo, hi = 50, N   # assume prefix 50 OK, N crashes
if try_compile('/tmp/pref_test.onnx') if build_prefix(50, '/tmp/pref_test.onnx') is None else False:
    pass
build_prefix(50, '/tmp/pref_test.onnx')
if try_compile('/tmp/pref_test.onnx'):
    print('prefix 50 OK')
else:
    print('prefix 50 already crashes; tighten')
    sys.exit(1)

while hi - lo > 1:
    mid = (lo + hi) // 2
    build_prefix(mid, '/tmp/pref_test.onnx')
    ok = try_compile('/tmp/pref_test.onnx')
    print(f'prefix {mid}: {"OK" if ok else "CRASH"}', flush=True)
    if ok:
        lo = mid
    else:
        hi = mid

build_prefix(hi, '/tmp/pref_crash.onnx')
mm = onnx.load('/tmp/pref_crash.onnx')
bad = mm.graph.node[-1]
print('first crashing node:', bad.op_type, bad.name)
# also print the few nodes before
for n in mm.graph.node[-6:-1]:
    print('  before:', n.op_type, n.name)
