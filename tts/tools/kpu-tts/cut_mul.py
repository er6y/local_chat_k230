import numpy as np, onnx, onnxruntime as ort
from onnx import TensorProto
SRC = '/tmp/matcha-icefall-zh-baker/matcha_C128n.onnx'
DST = '/tmp/matcha-icefall-zh-baker/matcha_C128m.onnx'
m = onnx.load(SRC)
G = m.graph
mul = next(n for n in G.node if n.name == '/decoder/Mul')
rnl_out = mul.input[0]
print('Mul:', mul.name, '<-', list(mul.input), '->', list(mul.output))
# consumers of Mul output -> 'noise' (already declared input [1,80,128])
for n in G.node:
    for j, i in enumerate(n.input):
        if i == mul.output[0]:
            n.input[j] = 'noise'
            print('rewired', n.op_type, n.name)
G.node.remove(mul)
# drop noise_scale input if now unused
used = {i for n in G.node for i in n.input}
names = [i.name for i in G.input]
keep = [i for i in G.input if i.name in used or i.name == 'noise']
del G.input[:]
G.input.extend(keep)
print('inputs now:', [i.name for i in G.input])
onnx.save(m, DST)
# verify vs C128n at ns=0.667 with matched noise (noise_m = noise_n * ns)
rng = np.random.default_rng(0)
mm = (rng.normal(0, 1, (1, 128, 80)) * 0.3).astype(np.float32)
du = np.full((1, 1, 256), 8.0, np.float32); du[0, 0, 200:] = 0.0
ns_v = 0.667
sn = ort.InferenceSession(SRC, providers=['CPUExecutionProvider'])
sm = ort.InferenceSession(DST, providers=['CPUExecutionProvider'])
raw = rng.normal(0, 1, (1, 80, 128)).astype(np.float32)
fn = {'noise': raw, '/MatMul_output_0': mm, '/Cast_3_output_0': np.ones((1, 1, 128), np.float32),
      '/Mul_1_output_0': du, 'noise_scale': np.array([ns_v], np.float32)}
fm = {'noise': (raw * ns_v).astype(np.float32), '/MatMul_output_0': mm,
      '/Cast_3_output_0': np.ones((1, 1, 128), np.float32), '/Mul_1_output_0': du}
a = sn.run(None, fn)[0]
b = sm.run(None, fm)[0]
print('C128m vs C128n (matched noise):', float(np.abs(a - b).max()))
