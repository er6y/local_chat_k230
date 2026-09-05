#!/usr/bin/env python3
# pin_iter.py — iterate: pin ALL -1 Reshape constants using RUNTIME-PROBED
# input shapes of the CURRENT graph (bit-exact by construction), then compile.
# Each round fixes the next poison; loop until the PTQ compile passes.
import os
import subprocess
import sys
import numpy as np
import onnx
import onnxruntime as ort
from onnx import numpy_helper, TensorProto, shape_inference

G_PATH = '/tmp/matcha-icefall-zh-baker/matcha_C128m.onnx'
KM = '/tmp/kpu_poc/matcha_AB/matcha_C128.kmodel'
COMPILE_PY = '/mnt/d/work/git_dev/k230_prj/tmp/mk_C_m.py'


def pin_rounds():
    for rnd in range(12):
        m = onnx.load(G_PATH)
        inits = {i.name: i for i in m.graph.initializer}
        # probe input shapes of every -1 reshape via one ORT run
        targets = [n for n in m.graph.node
                   if n.op_type == 'Reshape' and len(n.input) > 1
                   and n.input[1] in inits
                   and -1 in numpy_helper.to_array(inits[n.input[1]]).tolist()]
        print(f'round {rnd}: {-1} reshapes remaining = {len(targets)}', flush=True)
        if not targets:
            break
        mrun = onnx.load(G_PATH)
        need = list({n.input[0] for n in targets})
        msi = shape_inference.infer_shapes(onnx.load(G_PATH), strict_mode=False)
        vt = {v.name: v.type.tensor_type.elem_type for v in msi.graph.value_info}
        for o in need:
            mrun.graph.output.append(onnx.helper.make_tensor_value_info(
                o, vt.get(o, TensorProto.FLOAT), None))
        srun = ort.InferenceSession(mrun.SerializeToString(),
                                    providers=['CPUExecutionProvider'])
        g = np.random.default_rng(0)
        feed = {}
        for i in srun.get_inputs():
            shp = [d if isinstance(d, int) and d > 0 else 1 for d in i.shape] or [1]
            feed[i.name] = g.normal(0, 0.3, shp).astype(np.float32)
        vals = srun.run(need, feed)
        smap = {o: list(v.shape) for o, v in zip(need, vals)}

        pinned = 0
        for n in targets:
            ds = smap.get(n.input[0])
            arr = numpy_helper.to_array(inits[n.input[1]]).tolist()
            if not ds:
                continue
            total = int(np.prod(ds))
            others = [a for a in arr if a != -1]
            known = int(np.prod(others)) if others else 1
            if arr.count(-1) != 1 or known <= 0 or total % known != 0:
                continue
            new = [total // known if a == -1 else a for a in arr]
            inits[n.input[1]] = numpy_helper.from_array(np.array(new, np.int64),
                                                        n.input[1])
            pinned += 1
        keep = [inits[i.name] for i in m.graph.initializer]
        del m.graph.initializer[:]
        m.graph.initializer.extend(keep)
        del m.graph.value_info[:]
        onnx.save(m, G_PATH)
        print(f'round {rnd}: pinned {pinned}', flush=True)

        # numeric sanity: pinned graph vs previous run identical inputs
        s2 = ort.InferenceSession(G_PATH, providers=['CPUExecutionProvider'])
        outs = s2.run(None, feed)
        print(f'round {rnd}: post-pin run OK, out {outs[0].shape}', flush=True)

        r = subprocess.run([sys.executable, COMPILE_PY],
                           capture_output=True, text=True, timeout=3600)
        ok = r.returncode == 0 and 'KMODEL ->' in r.stdout
        print(f'round {rnd}: compile {"PASS" if ok else "crash"}', flush=True)
        if ok:
            print('DONE', flush=True)
            return
    print('EXHAUSTED', flush=True)


pin_rounds()
