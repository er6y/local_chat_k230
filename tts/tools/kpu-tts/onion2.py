#!/usr/bin/env python3
# onion2.py — checkpointing recursive PTQ fragment splitter (runs to completion,
# resumable; fragments + chain manifest in /tmp/kpu_poc/matcha_onionq).
import os
import subprocess
import sys
import json
import numpy as np
import onnx
import onnxruntime as ort
from onnx import TensorProto, shape_inference

BASE = '/tmp/matcha-icefall-zh-baker'
SRC = BASE + '/matcha_C128xs.onnx'
WORK = '/tmp/kpu_poc/matcha_onionq'
STATE = os.path.join(WORK, 'state.json')
COMPILE_PY = '/mnt/d/work/git_dev/k230_prj/tmp/try_parts_q.py'
os.makedirs(WORK, exist_ok=True)

_src = onnx.load(SRC)
ALL_NODES = list(_src.graph.node)
INIT_NAMES = {i.name for i in _src.graph.initializer}
INITS = {i.name: i for i in _src.graph.initializer}
NODE_BY_OUT = {}
for n in ALL_NODES:
    for o in n.output:
        NODE_BY_OUT[o] = n


def compile_ok(path, km):
    r = subprocess.run([sys.executable, COMPILE_PY, path, km],
                       capture_output=True, text=True, timeout=2400)
    ok = r.returncode == 0 and 'OK ->' in r.stdout
    print(f'    compile {os.path.basename(path)}: {"OK" if ok else "crash"}',
          flush=True)
    return ok


def write_fragment(nodes, inputs, out_names, path, tshape, vistyp):
    m2 = onnx.ModelProto()
    m2.CopyFrom(onnx.load(SRC))
    gg = m2.graph
    del gg.node[:]
    gg.node.extend(nodes)
    used = {i for n in nodes for i in n.input}
    del gg.initializer[:]
    gg.initializer.extend(i for i in INITS.values() if i.name in used)
    del gg.input[:]
    for nm, shp in inputs.items():
        gg.input.append(onnx.helper.make_tensor_value_info(nm, TensorProto.FLOAT, shp))
    del gg.output[:]
    for o in out_names:
        gg.output.append(onnx.helper.make_tensor_value_info(
            o, vistyp.get(o, TensorProto.FLOAT), tshape.get(o)))
    del gg.value_info[:]
    produced = set(i.name for i in gg.initializer)
    produced.update(inputs)
    rem = list(gg.node)
    srt = []
    while rem:
        for n in list(rem):
            if all(i in produced or i == '' for i in n.input):
                srt.append(n)
                rem.remove(n)
                produced.update(n.output)
                break
        else:
            raise RuntimeError('unsat ' + rem[0].name)
    del gg.node[:]
    gg.node.extend(srt)
    onnx.save(m2, path)


def probe_shapes(nodes, inputs):
    """ORT run of the fragment (or full graph) to get true output shapes."""
    outs = []
    seen = set()
    for n in nodes:
        for o in n.output:
            if o and o not in seen and o not in INIT_NAMES:
                seen.add(o)
                outs.append(o)
    write_fragment(nodes, inputs, outs, '/tmp/_probe.onnx', {}, {})
    s = ort.InferenceSession('/tmp/_probe.onnx', providers=['CPUExecutionProvider'])
    feed = {}
    for i in s.get_inputs():
        nm = i.name
        if nm == 'noise_scale':
            feed[nm] = np.array([0.667], np.float32)
        elif 'MatMul' in nm:
            feed[nm] = (np.random.default_rng(0).normal(0, 1, (1, 128, 80)) * 0.3).astype(np.float32)
        elif 'Cast_3' in nm:
            feed[nm] = np.ones((1, 1, 128), np.float32)
        elif 'Mul_1' in nm:
            du = np.full((1, 1, 256), 8.0, np.float32)
            du[0, 0, 200:] = 0.0
            feed[nm] = du
        else:
            shp = [d if isinstance(d, int) and d > 0 else 1 for d in i.shape] or [1]
            feed[nm] = np.random.default_rng(1).normal(0, 0.3, shp).astype(np.float32)
    vals = s.run(outs, feed)
    return {o: list(v.shape) for o, v in zip(outs, vals)}, outs


def bisect_crash(nodes, inputs, tag, tshape, vistyp):
    """smallest crashing prefix index (1-based) via binary PTQ compiles."""
    def test(k):
        p = os.path.join(WORK, f'_{tag}_p{k}.onnx')
        km = p.replace('.onnx', '.kmodel')
        outs = [o for o in nodes[k - 1].output if o][:1]
        write_fragment(nodes[:k], inputs, outs, p, tshape, vistyp)
        ok = compile_ok(p, km)
        for f in (p, km):
            if os.path.exists(f):
                os.remove(f)
        return ok

    lo, hi = 1, len(nodes)
    if test(hi):
        return None
    while hi - lo > 1:
        mid = (lo + hi) // 2
        if test(mid):
            lo = mid
        else:
            hi = mid
    return hi


def split(tag, node_range, inputs):
    """process fragment nodes[r0:r1); recurse on crash; record done pieces."""
    nodes = ALL_NODES[node_range[0]:node_range[1]]
    if not nodes:
        return []
    p = os.path.join(WORK, f'{tag}.onnx')
    km = p.replace('.onnx', '.kmodel')
    final_out = node_range[1] == len(ALL_NODES)
    outs = (['/decoder/Add_2_output_0'] if final_out
            else [o for o in nodes[-1].output if o][:1])
    tshape, _ = probe_shapes(ALL_NODES[node_range[0]:node_range[1]], inputs)
    write_fragment(nodes, inputs, outs, p, tshape, {})
    if os.path.exists(km) and os.path.getsize(km) > 0:
        print(f'{tag}: kmodel exists, skip', flush=True)
        return [(tag, node_range)]
    if compile_ok(p, km):
        print(f'{tag}: COMPILED ({len(nodes)} nodes)', flush=True)
        return [(tag, node_range)]
    print(f'{tag}: crash; bisect', flush=True)
    crash_k = bisect_crash(nodes, inputs, tag, tshape, {})
    if crash_k is None:
        return [(tag, node_range)]
    k = node_range[0] + crash_k - 1   # global boundary: nodes[:k] ok
    if len(nodes) == 1:
        # indivisible poison node: execute on CPU in the runner (record it)
        print(f'{tag}: CPU-OP bypass: {nodes[0].op_type} {nodes[0].name}', flush=True)
        return [(tag, node_range, 'cpu')]
    print(f'{tag}: split at global {k}', flush=True)
    # boundary tensors
    outsA = {o for n in ALL_NODES[node_range[0]:k] for o in n.output}
    partB = ALL_NODES[k:node_range[1]]
    bnd = sorted({i for n in partB for i in n.input if i in outsA})
    tshape_all, _ = probe_shapes(ALL_NODES, {i.name: [d.dim_value for d in i.type.tensor_type.shape.dim] for i in _src.graph.input})
    inputsA = {}
    needA = sorted({i for n in ALL_NODES[node_range[0]:k] for i in n.input
                    if i and i not in outsA and i not in INIT_NAMES})
    base_in = {i.name: [d.dim_value for d in i.type.tensor_type.shape.dim] for i in _src.graph.input}
    for nm in needA:
        inputsA[nm] = base_in.get(nm) or tshape_all.get(nm)
    inputsB = {b: tshape_all.get(b) for b in bnd}
    for nm in base_in:
        if nm in {i for n in partB for i in n.input}:
            inputsB.setdefault(nm, base_in[nm])
    left = split(tag + 'a', (node_range[0], k), inputsA)
    right = split(tag + 'b', (k, node_range[1]), inputsB)
    return left + right


base_in = {i.name: [d.dim_value for d in i.type.tensor_type.shape.dim] for i in _src.graph.input}
pieces = split('f', (0, len(ALL_NODES)), base_in)
print('ALL PIECES:', pieces)
with open(os.path.join(WORK, 'manifest.json'), 'w') as f:
    json.dump({'pieces': [list(p) for p in pieces]}, f)
print('manifest written')
