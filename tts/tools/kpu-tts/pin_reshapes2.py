#!/usr/bin/env python3
# pin_reshapes2.py — rebuild pristine C128, probe TRUE runtime shapes from the
# dynamic C graph at an exact 128-frame run, then pin every -1 Reshape.
import numpy as np
import onnx
import onnxruntime as ort
from onnx import numpy_helper

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
CD = '/tmp/matcha-icefall-zh-baker/matcha_C_dyn.onnx'
PC = '/tmp/matcha-icefall-zh-baker/matcha_C128.onnx'

# ---- 1) exact-128 run of the ORIGINAL graph for ground-truth intermediates
m2 = onnx.load(SRC)
for w in ['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0']:
    m2.graph.output.append(onnx.helper.make_tensor_value_info(w, onnx.TensorProto.FLOAT, None))
s2 = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
rng = np.random.default_rng(3)
tok = rng.integers(4, 2000, 10).astype(np.int64)
ids = np.asarray([1] + [v for t in tok for v in (int(t), 1)], np.int64)
_, _, dur1 = s2.run(['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0'],
                    {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
                     'noise_scale': np.array([0.0], np.float32),
                     'length_scale': np.array([1.0], np.float32)})
Lp0 = int(round(dur1.sum()))
ls = 128.0 / Lp0
mm_o, c3_o, dur_o = s2.run(
    ['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0'],
    {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
     'noise_scale': np.array([0.0], np.float32),
     'length_scale': np.array([ls], np.float32)})
assert mm_o.shape[1] == 128, mm_o.shape
Db = np.zeros((1, 1, 256), np.float32)
Db[0, 0, :dur_o.shape[2]] = dur_o[0]

# ---- 2) probe ALL node outputs of C-dyn at this exact input
md = onnx.load(CD)
all_outs = []
seen = set()
for n in md.graph.node:
    for o in n.output:
        if o and o not in seen:
            seen.add(o)
            all_outs.append(o)
md2 = onnx.load(CD)
for o in all_outs:
    md2.graph.output.append(onnx.helper.make_tensor_value_info(o, onnx.TensorProto.FLOAT, None))
# static shape inference on the STATIC C128 graph itself (all inputs pinned),
# then a single ORT run with every tensor as a correctly-typed extended output
mms = onnx.load(PC)
mms = onnx.shape_inference.infer_shapes(mms, strict_mode=False)
vis = {v.name: v for v in mms.graph.value_info}
from onnx import TensorProto
def et_of(name):
    v = vis.get(name)
    return v.type.tensor_type.elem_type if v is not None else TensorProto.FLOAT
mrun = onnx.load(PC)
# only the tensors we need: data inputs of -1 reshapes
mneed = onnx.load(PC)
inits_n = {i.name for i in mneed.graph.initializer}
need = []
for n in mneed.graph.node:
    if n.op_type == 'Reshape' and n.input[1] in inits_n:
        from onnx import numpy_helper as _nh
        arr = _nh.to_array(next(i for i in mneed.graph.initializer if i.name == n.input[1])).tolist()
        if -1 in arr:
            need.append(n.input[0])
mmsi = onnx.shape_inference.infer_shapes(onnx.load(PC), strict_mode=False)
vis2 = {v.name: v for v in mmsi.graph.value_info}
from onnx import TensorProto as _TP
for o in need:
    v = vis2.get(o)
    et = v.type.tensor_type.elem_type if v is not None else _TP.FLOAT
    mrun.graph.output.append(onnx.helper.make_tensor_value_info(o, et, None))
try:
    srun = ort.InferenceSession(mrun.SerializeToString(), providers=['CPUExecutionProvider'])
    vals = srun.run(need, {'/MatMul_output_0': mm_o, '/Cast_3_output_0': c3_o,
                           '/Mul_1_output_0': Db,
                           'noise_scale': np.array([0.0], np.float32)})
    true_shape = {o: v.shape for o, v in zip(need, vals)}
    print('probed reshape-data tensors:', len(true_shape))
except Exception as e:
    print('probe failed:', str(e)[:200])
    true_shape = {}

# ---- 3) pin -1 reshapes in C128 using true shapes
m = onnx.load(PC)
inits = {i.name: i for i in m.graph.initializer}
changed = skipped = 0
for n in m.graph.node:
    if n.op_type != 'Reshape':
        continue
    sn = n.input[1]
    if sn not in inits:
        continue
    arr = numpy_helper.to_array(inits[sn]).tolist()
    if -1 not in arr:
        continue
    ds = true_shape.get(n.input[0])
    if ds is None:
        skipped += 1
        continue
    total = int(np.prod(ds))
    known = int(np.prod([a for a in arr if a != -1])) if len([a for a in arr if a != -1]) else 1
    if known == 0 or total % known != 0:
        skipped += 1
        continue
    new = [total // known if a == -1 else a for a in arr]
    inits[sn] = numpy_helper.from_array(np.array(new, np.int64), sn)
    changed += 1
print('pinned:', changed, 'skipped:', skipped)

out_inits = [inits[i.name] for i in m.graph.initializer]
del m.graph.initializer[:]
m.graph.initializer.extend(out_inits)
onnx.save(m, PC)

# verify numerics intact
sC = ort.InferenceSession(PC, providers=['CPUExecutionProvider'])
mel_c = sC.run(None, {'/MatMul_output_0': mm_o, '/Cast_3_output_0': c3_o,
                      '/Mul_1_output_0': Db,
                      'noise_scale': np.array([0.0], np.float32)})[0]
mel_o = ort.InferenceSession(SRC, providers=['CPUExecutionProvider']).run(
    ['mel'], {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
              'noise_scale': np.array([0.0], np.float32),
              'length_scale': np.array([ls], np.float32)})[0]
d = np.abs(mel_c - mel_o).max()
print('post-pin verify maxdiff:', d, '->', 'OK' if d < 5e-3 else 'FAIL')
