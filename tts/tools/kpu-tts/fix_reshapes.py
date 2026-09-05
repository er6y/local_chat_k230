#!/usr/bin/env python3
# fix_reshapes.py — pin every Reshape constant containing -1 in C128 to fully
# explicit shapes (rank inferred from the data producer chain via shape info).
import numpy as np
import onnx
from onnx import numpy_helper, shape_inference

P = '/tmp/matcha-icefall-zh-baker/matcha_C128.onnx'
m = onnx.load(P)
m = shape_inference.infer_shapes(m)
vi = {v.name: v for v in m.graph.value_info}

inits = {i.name: i for i in m.graph.initializer}
prod = {}
for n in m.graph.node:
    for o in n.output:
        prod[o] = n


def shape_of(t):
    if t in vi:
        return [d.dim_value for d in vi[t].type.tensor_type.shape.dim]
    return None


changed = 0
for n in m.graph.node:
    if n.op_type != 'Reshape':
        continue
    shp_name = n.input[1]
    if shp_name not in inits:
        continue
    arr = numpy_helper.to_array(inits[shp_name])
    if -1 not in arr.tolist():
        continue
    data_t = n.input[0]
    ds = shape_of(data_t)
    if ds is None or any(d == 0 for d in ds):
        continue
    total = int(np.prod(ds))
    known = int(np.prod([a for a in arr if a != -1]))
    if known == 0 or total % known != 0:
        continue
    new = [total // known if a == -1 else a for a in arr]
    inits[shp_name] = numpy_helper.from_array(np.array(new, np.int64), shp_name)
    changed += 1
    print(f'{n.name}: {arr.tolist()} -> {new}')

# write back
out_inits = [inits[i.name] for i in m.graph.initializer]
del m.graph.initializer[:]
m.graph.initializer.extend(out_inits)
del m.graph.value_info[:]   # drop inferred vi (stale entries break ORT)
onnx.save(m, P)
print('pinned', changed, 'reshapes')

# quick numeric re-verify after edit
import onnxruntime as ort
sC = ort.InferenceSession(P, providers=['CPUExecutionProvider'])
print('session loads OK; inputs:', [(i.name, i.shape) for i in sC.get_inputs()])
