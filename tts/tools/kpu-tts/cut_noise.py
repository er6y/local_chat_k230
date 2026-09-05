#!/usr/bin/env python3
# cut_noise.py — remove the RandomNormalLike (the PTQ-pass poison node) from
# C128xs: replace with a graph input 'noise'; runner supplies N(0,1) samples.
# Verify at ns=0 (noise zeroed -> deterministic) vs C-dyn.
import numpy as np
import onnx
import onnxruntime as ort
from onnx import TensorProto, shape_inference

SRC = '/tmp/matcha-icefall-zh-baker/matcha_C128xs.onnx'
DST = '/tmp/matcha-icefall-zh-baker/matcha_C128n.onnx'

m = onnx.load(SRC)
G = m.graph
rnl = next(n for n in G.node if n.op_type == 'RandomNormalLike')
print('RNL:', rnl.name, '<-', list(rnl.input), '->', list(rnl.output))
# input = Transpose_3_output_0 shape? probe via ORT shape inference + runtime
msi = shape_inference.infer_shapes(onnx.load(SRC), strict_mode=False)
vis = {v.name: v for v in msi.graph.value_info}
shp_t = rnl.input[0]
vi = vis.get(shp_t)
dims = [d.dim_value for d in vi.type.tensor_type.shape.dim] if vi else None
print('noise shape from vi:', dims)
# runtime probe to be sure
mrun = onnx.load(SRC)
mrun.graph.output.append(onnx.helper.make_tensor_value_info(rnl.output[0], TensorProto.FLOAT, None))
if vi is None:
    mrun.graph.output.append(onnx.helper.make_tensor_value_info(shp_t, TensorProto.FLOAT, None))
srun = ort.InferenceSession(mrun.SerializeToString(), providers=['CPUExecutionProvider'])
rng = np.random.default_rng(0)
mm = (rng.normal(0, 1, (1, 128, 80)) * 0.3).astype(np.float32)
du = np.full((1, 1, 256), 8.0, np.float32)
du[0, 0, 200:] = 0.0
names = [rnl.output[0]] + ([shp_t] if vi is None else [])
outs = srun.run(names, {'/MatMul_output_0': mm, '/Cast_3_output_0': np.ones((1, 1, 128), np.float32),
                        '/Mul_1_output_0': du, 'noise_scale': np.array([0.667], np.float32)})
true_shape = list(outs[0].shape)
print('noise runtime shape:', true_shape)

# remove the node, rewire consumers to new input 'noise'
G.input.append(onnx.helper.make_tensor_value_info('noise', TensorProto.FLOAT, true_shape))
for n in G.node:
    for j, i in enumerate(n.input):
        if i == rnl.output[0]:
            n.input[j] = 'noise'
G.node.remove(rnl)
del G.value_info[:]
onnx.save(m, DST)
print('saved', DST)

# deterministic verify at ns=0: C128n vs C-dyn (both zero noise)
sN = ort.InferenceSession(DST, providers=['CPUExecutionProvider'])
sO = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/model-steps-3.onnx',
                          providers=['CPUExecutionProvider'])
tok0 = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
ids = np.asarray([1] + [v for t in tok0 for v in (t, 1)], np.int64)
L = len(ids)

def orig(ls):
    return sO.run(['mel'], {'x': ids[None, :], 'x_length': np.array([L], np.int64),
                            'noise_scale': np.array([0.0], np.float32),
                            'length_scale': np.array([ls], np.float32)})[0]

mE = onnx.load('/tmp/matcha-icefall-zh-baker/model-steps-3.onnx')
for w in ['/Mul_1_output_0', '/MatMul_output_0']:
    mE.graph.output.append(onnx.helper.make_tensor_value_info(w, TensorProto.FLOAT, None))
sE = ort.InferenceSession(mE.SerializeToString(), providers=['CPUExecutionProvider'])
dur1, _ = sE.run(['/Mul_1_output_0', '/MatMul_output_0'],
                 {'x': ids[None, :], 'x_length': np.array([L], np.int64),
                  'noise_scale': np.array([0.0], np.float32),
                  'length_scale': np.array([1.0], np.float32)})
Lp0 = int(round(dur1.sum()))
ls = 128.0 / Lp0
mel_o = orig(ls)
_, mm_o = sE.run(['/Mul_1_output_0', '/MatMul_output_0'],
                 {'x': ids[None, :], 'x_length': np.array([L], np.int64),
                  'noise_scale': np.array([0.0], np.float32),
                  'length_scale': np.array([ls], np.float32)})
dur_o, _ = sE.run(['/Mul_1_output_0', '/MatMul_output_0'],
                  {'x': ids[None, :], 'x_length': np.array([L], np.int64),
                   'noise_scale': np.array([0.0], np.float32),
                   'length_scale': np.array([ls], np.float32)})
Db = np.zeros((1, 1, 256), np.float32)
Db[0, 0, :dur_o.shape[2]] = dur_o[0]
noise = np.zeros(true_shape, np.float32)   # ns=0 semantics: mul by 0
mel_n = sN.run(None, {'noise': noise, '/MatMul_output_0': mm_o,
                      '/Cast_3_output_0': np.ones((1, 1, 128), np.float32),
                      '/Mul_1_output_0': Db,
                      'noise_scale': np.array([0.0], np.float32)})[0]
d = np.abs(mel_n - mel_o)
print(f'C128n vs original@exact128 ns=0: maxdiff={d.max():.6f} -> '
      f'{"OK" if d.max() < 5e-3 else "FAIL"}')
