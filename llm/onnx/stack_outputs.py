import sys
import onnx
from onnx import helper, TensorProto

SRC = sys.argv[1] if len(sys.argv) > 1 else 'qwen25_24l_s1h256_kv6.onnx'
DST = sys.argv[2] if len(sys.argv) > 2 else 'qwen25_24l_s1h256_kv6_stacked.onnx'
m = onnx.load(SRC)
g = m.graph
names = [o.name for o in g.output]
kt = [n for n in names if n.startswith('ktnew')]
vt = [n for n in names if n.startswith('vtnew')]
assert len(kt) == 48 and len(vt) == 48, (len(kt), len(vt))
# 保持原输出顺序（l*2+g 交错）
new_outputs = [o for o in g.output if o.name == 'logits']

def stack(names_list, out_name):
    ins = [n for n in names_list]
    node = helper.make_node('Concat', ins, [out_name], axis=0, name='concat_' + out_name)
    g.node.append(node)
    new_outputs.append(helper.make_tensor_value_info(out_name, TensorProto.FLOAT, [48, 1, 1, 64]))

stack(kt, 'ktnew_all')
stack(vt, 'vtnew_all')
del g.output[:]
g.output.extend(new_outputs)
onnx.save(m, DST)
print('saved, outputs:', [o.name for o in g.output])
