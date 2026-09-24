# server: extract layer-0 slice from kv6_stacked onnx -> s3c.onnx
# keeps ALL 5 graph inputs with original shapes (kt_all [48,64,32] etc.), same Slices
import onnx
from onnx import helper, TensorProto as T, shape_inference
import numpy as np

m = onnx.load('qwen25_24l_s1h256_kv6_stacked.onnx')
g = m.graph
nodes = list(g.node)

# layer-1 boundary = 2nd input_layernorm node
ln_idx = [i for i, n in enumerate(nodes) if n.op_type == 'Slice' and n.name.startswith('kv6sk_1_')]
print('input_layernorm nodes at:', ln_idx[:5])
cut = ln_idx[0] - 7
kept = nodes[:cut]
print('kept nodes [0..%d) = %d nodes' % (cut, len(kept)))

inames = [i.name for i in g.input]
consumed = set()
for n in kept:
    consumed.update(n.input)
dangling = []
for n in kept:
    for o in n.output:
        if o and o not in consumed and o not in dangling:
            dangling.append(o)
print('dangling (feeds dropped nodes):', len(dangling))

# shapes via shape inference on full model once
full = shape_inference.infer_shapes(m)
vis = {}
for vi in list(full.graph.value_info) + list(full.graph.output):
    vis[vi.name] = vi

new_outputs = []
for name in dangling:
    vi = vis.get(name)
    if vi is None:
        print('NO SHAPE for dangling', name, '- skipping output (may DCE)')
        continue
    new_outputs.append(vi)
print('forced outputs:', len(new_outputs))

ng = helper.make_graph(
    kept, 's3c',
    list(g.input),
    new_outputs,
    list(g.initializer))
nm = helper.make_model(ng, opset_imports=list(m.opset_import))
nm.ir_version = m.ir_version
nm.producer_name = 's3c-cut'
onnx.save(nm, 's3c.onnx')
print('s3c.onnx saved: nodes=%d outputs=%d' % (len(kept), len(new_outputs)))
