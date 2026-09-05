import numpy as np, onnx, onnxruntime as ort
from onnx import numpy_helper, TensorProto, shape_inference
P = '/tmp/matcha-icefall-zh-baker/matcha_C128m.onnx'
OUT = '/tmp/matcha-icefall-zh-baker/matcha_C128t.onnx'
m = onnx.load(P)
G = m.graph
inits = {i.name for i in G.initializer}
node_outs, seen = [], set()
for n in G.node:
    for o in n.output:
        if o and o not in seen and o not in inits:
            seen.add(o); node_outs.append(o)
msi = shape_inference.infer_shapes(onnx.load(P), strict_mode=False)
vt = {v.name: v.type.tensor_type.elem_type for v in msi.graph.value_info}
mrun = onnx.load(P)
for o in node_outs:
    mrun.graph.output.append(onnx.helper.make_tensor_value_info(o, vt.get(o, TensorProto.FLOAT), None))
srun = ort.InferenceSession(mrun.SerializeToString(), providers=['CPUExecutionProvider'])
g = np.random.default_rng(0)
feed = {}
for i in srun.get_inputs():
    shp = [d if isinstance(d, int) and d > 0 else 1 for d in i.shape] or [1]
    feed[i.name] = g.normal(0, 0.3, shp).astype(np.float32)
vals = srun.run(node_outs, feed)
ts = {o: list(v.shape) for o, v in zip(node_outs, vals)}
fixed = same = 0
iarr = {i.name: numpy_helper.to_array(i) for i in G.initializer}
for n in G.node:
    if n.op_type != 'Reshape' or len(n.input) < 2 or n.input[1] not in inits:
        continue
    t = ts.get(n.output[0])
    if not t:
        continue
    if iarr[n.input[1]].tolist() == t:
        same += 1
        continue
    iarr[n.input[1]] = np.array(t, np.int64)
    fixed += 1
print('fixed:', fixed, 'already-true:', same)
keep = []
for i in G.initializer:
    keep.append(numpy_helper.from_array(iarr[i.name].copy(), i.name)
                if isinstance(iarr[i.name], np.ndarray) else i)
del G.initializer[:]
G.initializer.extend(keep)
del G.value_info[:]
onnx.save(m, OUT)
# ORT load test + numeric identity vs pre-fix
sa = ort.InferenceSession(P, providers=['CPUExecutionProvider'])
sb = ort.InferenceSession(OUT, providers=['CPUExecutionProvider'])
a = sa.run(None, feed)[0]
b = sb.run(None, feed)[0]
print('identity maxdiff:', float(np.abs(a - b).max()))
