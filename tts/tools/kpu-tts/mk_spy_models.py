#!/usr/bin/env python3
# mk_spy_models.py — tiny "spy" ONNX models that reveal the official frontend's
# token stream: spy_acoustic embeds x (token ids) into mel channel 0; spy_vocoder
# passes mel channel 0 through as "audio". The generate() result = token ids.
import numpy as np
import onnx
from onnx import helper, TensorProto, numpy_helper

# ---- spy acoustic: mel[N,80,L] where mel[:,0,:L] = cast(x), rest zeros ----
x = helper.make_tensor_value_info('x', TensorProto.INT64, ['N', 'L'])
xl = helper.make_tensor_value_info('x_length', TensorProto.INT64, ['N'])
ns = helper.make_tensor_value_info('noise_scale', TensorProto.FLOAT, [1])
ls = helper.make_tensor_value_info('length_scale', TensorProto.FLOAT, [1])
mel = helper.make_tensor_value_info('mel', TensorProto.FLOAT, ['N', 80, 'L'])

n0 = helper.make_node('Cast', ['x'], ['xf'], to=TensorProto.FLOAT)
n1 = helper.make_node('Unsqueeze', ['xf', 'ax0'], ['xu'])  # [N,1,L]
# zeros [N,79,L] = 0 * xu tiled (same rank for Concat)
nodes = [n0, n1,
         helper.make_node('Mul', ['xu', 'zero'], ['z1'])]
zin = ['z1'] * 79
n2 = helper.make_node('Concat', ['xu'] + zin, ['mel'], axis=1)  # [N,80,L]
nodes.append(n2)
g = helper.make_graph(
    nodes,
    'spy_acoustic',
    [x, xl, ns, ls],
    [mel],
    initializer=[
        numpy_helper.from_array(np.array([1], np.int64), 'ax0'),
        numpy_helper.from_array(np.array(0.0, np.float32), 'zero'),
    ],
)
m = helper.make_model(g, opset_imports=[helper.make_opsetid('', 13)])
m.ir_version = 8
# copy metadata from the real acoustic (sherpa reads sample_rate etc.)
real = onnx.load('/tmp/matcha-icefall-zh-baker/model-steps-3.onnx')
for p in real.metadata_props:
    m.metadata_props.append(p)
onnx.save(m, '/tmp/matcha-icefall-zh-baker/spy_acoustic.onnx')
print('spy_acoustic saved')

# ---- spy vocoder: audio[N, L] = mel[N, 0, L] ----
mi = helper.make_tensor_value_info('mel', TensorProto.FLOAT, ['N', 80, 'L'])
ao = helper.make_tensor_value_info('audio', TensorProto.FLOAT, ['N', 'L'])
s0 = helper.make_node('Slice', ['mel', 'st', 'en', 'ax1', 'stp'], ['m0'])  # noqa  # [N,1,L]
s1 = helper.make_node('Squeeze', ['m0', 'ax1b'], ['audio'])                # [N,L]
g2 = helper.make_graph(
    [s0, s1],
    'spy_vocoder',
    [mi],
    [ao],
    initializer=[
        numpy_helper.from_array(np.array([0], np.int64), 'st'),
        numpy_helper.from_array(np.array([1], np.int64), 'en'),
        numpy_helper.from_array(np.array([1], np.int64), 'ax1'),
        numpy_helper.from_array(np.array([1], np.int64), 'stp'),
        numpy_helper.from_array(np.array([1], np.int64), 'ax1b'),
    ],
)
m2 = helper.make_model(g2, opset_imports=[helper.make_opsetid('', 13)])
m2.ir_version = 8
# sherpa's vocoder factory requires model_type metadata ("hifigan")
meta = m2.metadata_props.add()
meta.key = 'model_type'
meta.value = 'hifigan'
onnx.save(m2, '/tmp/matcha-icefall-zh-baker/spy_vocoder.onnx')
print('spy_vocoder saved')
