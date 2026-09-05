#!/usr/bin/env python3
# surgery8_mask.py — inject key-axis attention masks into all 18 attn Softmax
# sites of C512 (mask512 from Cast_3; mask256 = MaxPool2(mask512)).
import numpy as np
import onnx
import onnxruntime as ort

PC = '/tmp/matcha-icefall-zh-baker/matcha_C512.onnx'
MASK = '/Cast_3_output_0'   # [1,1,512] graph input
NEG = -1e9

m = onnx.load(PC)
G = m.graph
prod = {}
for n in G.node:
    for o in n.output:
        prod[o] = n

# classify softmax sites by their score width
sites = [n for n in G.node if n.op_type == 'Softmax' and '/attn1' in (n.name or '')]
print('attn sites:', len(sites))

new_nodes = []
const_neg = onnx.helper.make_tensor_value_info('mask_neg_const', onnx.TensorProto.FLOAT, None)

# helper value producers appended as nodes
_made = {}
def mk_bias(width):
    """bias_Lx [1,1,Lx] = (mask_Lx - 1) * NEG; returns bias tensor name."""
    if width in _made:
        return _made[width]
    if width == 512:
        msrc = MASK
    else:
        # mask256 = MaxPool(mask512, k=2, s=2)
        mp = onnx.helper.make_node('MaxPool', [MASK], [f'mask_{width}'],
                                   name=f'mask_maxpool_{width}', kernel_shape=[2],
                                   strides=[2])
        new_nodes.append(mp)
        msrc = f'mask_{width}'
    sub = onnx.helper.make_node('Sub', [msrc, 'one_const'], [f'msub_{width}'],
                                name=f'mask_sub_{width}')
    mul = onnx.helper.make_node('Mul', [f'msub_{width}', 'neg_const'],
                                [f'mbias_{width}'], name=f'mask_mul_{width}')
    uns = onnx.helper.make_node('Unsqueeze', [f'mbias_{width}', 'ax3'],
                                [f'mbias4_{width}'], name=f'mask_unsq_{width}')
    new_nodes.extend([sub, mul, uns])
    _made[width] = f'mbias4_{width}'
    return _made[width]

inits_before = {i.name for i in G.initializer}
G.initializer.extend([
    onnx.numpy_helper.from_array(np.array(NEG, np.float32), 'neg_const'),
    onnx.numpy_helper.from_array(np.array(1.0, np.float32), 'one_const'),
    onnx.numpy_helper.from_array(np.array([3], np.int64), 'ax3'),
])

count = 0
for sm in sites:
    score_t = sm.input[0]
    p = prod.get(score_t)
    # width from the softmax producer chain: use the Add's first MatMul-ish
    # sibling — simpler: run-free heuristic by node name group is unreliable;
    # instead infer from any consumer of attn1/Mul_3 pattern — easiest is to
    # look at the Add's inputs for a MatMul output and get width later via ORT.
    # We instead inject generically and let broadcasting fail loudly if wrong.
    # Determine width from the estimator group: probe once outside.
    pass

# need widths: probe the CURRENT graph once
names = [sm.input[0] for sm in sites]
m2 = onnx.load(PC)
for w in names:
    m2.graph.output.append(onnx.helper.make_tensor_value_info(w, onnx.TensorProto.FLOAT, None))
s2 = ort.InferenceSession(m2.SerializeToString(), providers=['CPUExecutionProvider'])
mm = np.zeros((1, 512, 80), np.float32)
mask = np.ones((1, 1, 512), np.float32)
dur = np.zeros((1, 1, 256), np.float32)
outs = s2.run(names, {'/MatMul_output_0': mm, '/Cast_3_output_0': mask,
                      '/Mul_1_output_0': dur, 'noise_scale': np.array([0.0], np.float32)})
widths = {sm.input[0]: v.shape[-1] for sm, v in zip(sites, outs)}
print('width histogram:', {w: list(widths.values()).count(w) for w in set(widths.values())})

for sm in sites:
    score_t = sm.input[0]
    w = widths[score_t]
    bias = mk_bias(w)
    add = onnx.helper.make_node('Add', [score_t, bias],
                                [score_t + '_masked'], name=sm.name + '_maskadd')
    new_nodes.append(add)
    sm.input[0] = score_t + '_masked'
    count += 1

G.node.extend(new_nodes)
onnx.save(m, PC)
print('injected at', count, 'sites; extra nodes:', len(new_nodes))

# re-verify: C512 padded vs C-dyn exact
sC = ort.InferenceSession(PC, providers=['CPUExecutionProvider'])
sCd = ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C_dyn.onnx',
                           providers=['CPUExecutionProvider'])
SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
m3 = onnx.load(SRC)
for w in ['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0']:
    m3.graph.output.append(onnx.helper.make_tensor_value_info(w, onnx.TensorProto.FLOAT, None))
s3 = ort.InferenceSession(m3.SerializeToString(), providers=['CPUExecutionProvider'])
tok0 = np.fromfile('/tmp/kpu_poc/matcha_calib/tok_short.bin', np.int64)
ids = np.asarray([1] + [v for t in tok0 for v in (t, 1)], np.int64)
mm_o, c3_o, dur_o = s3.run(
    ['/MatMul_output_0', '/Cast_3_output_0', '/Mul_1_output_0'],
    {'x': ids[None, :], 'x_length': np.array([len(ids)], np.int64),
     'noise_scale': np.array([0.0], np.float32),
     'length_scale': np.array([1.0], np.float32)})
Lp = mm_o.shape[1]
mel_dyn = sCd.run(None, {'/MatMul_output_0': mm_o, '/Cast_3_output_0': c3_o,
                         '/Mul_1_output_0': dur_o,
                         'noise_scale': np.array([0.0], np.float32)})[0]
mm_p = np.zeros((1, 512, 80), np.float32)
mm_p[0, :Lp] = mm_o[0]
c3_p = np.zeros((1, 1, 512), np.float32)
c3_p[0, 0, :Lp] = 1.0
dur_p = np.zeros((1, 1, 256), np.float32)
dur_p[0, 0, :dur_o.shape[2]] = dur_o[0]
mel_pad = sC.run(None, {'/MatMul_output_0': mm_p, '/Cast_3_output_0': c3_p,
                        '/Mul_1_output_0': dur_p,
                        'noise_scale': np.array([0.0], np.float32)})[0]
d = np.abs(mel_pad[0, :, :Lp] - mel_dyn[0, :, :Lp])
print(f'padded vs exact after mask: maxdiff={d.max():.6f} mean={d.mean():.6f}')
