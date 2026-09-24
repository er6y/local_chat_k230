import sys
import onnx, numpy as np
from onnx import numpy_helper
SRC = sys.argv[1] if len(sys.argv) > 1 else '/root/qwen25_pipe/onnx/llm.onnx'
DST = sys.argv[2] if len(sys.argv) > 2 else '/root/qwen25_24l_split.onnx'
m = onnx.load(SRC, load_external_data=True)
g = m.graph
inits = {i.name: i for i in g.initializer}

# load full lm_head weight
w = numpy_helper.to_array(inits['/lm/lm_head/Linear_weight'])  # [896, 151936]
print('w', w.shape, w.dtype)
VOCAB = 151936
CHUNK = 50645  # 3 chunks: 50645*3=151935 -> need 4th of 1; use 50646*2+50644? simpler: ceil split into 3
chunks = [w[:, :50645], w[:, 50645:101290], w[:, 101290:]]
assert sum(c.shape[1] for c in chunks) == VOCAB
print('chunks', [c.shape[1] for c in chunks])

# find the matmul node
target = None
for n in g.node:
    if n.op_type == 'MatMul' and n.input[1] == '/lm/lm_head/Linear_weight':
        target = n; break
x = target.input[0]
out = target.output[0]

new_nodes = []
piece_outs = []
for i, c in enumerate(chunks):
    iname = f'/lm/lm_head/Linear_weight_chunk{i}'
    oname = f'/lm/lm_head_out_chunk{i}'
    g.initializer.append(numpy_helper.from_array(np.ascontiguousarray(c), iname))
    new_nodes.append(onnx.helper.make_node('MatMul', [x, iname], [oname], name=f'/lm/lm_head_chunk{i}'))
    piece_outs.append(oname)
new_nodes.append(onnx.helper.make_node('Concat', piece_outs, [out], axis=-1, name='/lm/lm_head_concat'))

# replace target node with new_nodes
idx = list(g.node).index(target)
del g.node[idx]
for j, nn in enumerate(new_nodes):
    g.node.insert(idx + j, nn)

# drop old weight init
g.initializer.remove(inits['/lm/lm_head/Linear_weight'])

m2 = m
onnx.save_model(m2, DST, save_as_external_data=False)
print('saved split-lmhead onnx')
