# specimen B: mimic kv6 host-input consumption pattern
# inputs: x[1,1,896], kall[2,64,32], vall[2,32,64]  (stacked over g like kv6 kt_all/v_all)
# graph: per g, Gather slice from host input -> GEMMs; tiny compute; 2 outputs
import numpy as np, onnx
from onnx import helper, TensorProto as T

nodes = []
inits = []
def init(name, arr):
    inits.append(helper.make_tensor(name, T.FLOAT, arr.shape, arr.flatten().tolist()))
    return name

# q projection: q = x @ Wq  -> [1,1,64]
Wq = init("Wq", (np.random.default_rng(1).standard_normal((896,64))*0.02).astype(np.float32))
nodes.append(helper.make_node("Reshape", ["x", "sh3"], ["x3"]))
nodes.append(helper.make_node("MatMul", ["x3", Wq], ["q"]))  # [1,896]x[896,64]->[1,64]
sh3 = helper.make_tensor("sh3", T.INT64, [2], [1,896])
inits.append(sh3)
sh4 = helper.make_tensor("sh4", T.INT64, [3], [1,1,64])
inits.append(sh4)

prev = None
for g in range(2):
    # in-graph slice of HOST input (the pattern under test)
    gi = helper.make_tensor("g%d" % g, T.INT64, [], [g])
    inits.append(gi)
    nodes.append(helper.make_node("Gather", ["kall", "g%d" % g], ["k%d" % g], axis=0))  # [64,32]
    nodes.append(helper.make_node("Gather", ["vall", "g%d" % g], ["v%d" % g], axis=0))  # [32,64]
    nodes.append(helper.make_node("MatMul", ["q", "k%d" % g], ["s%d" % g]))            # [1,32]
    # softmax over 32
    sn = helper.make_tensor("sn%d" % g, T.FLOAT, [], [32.0])
    inits.append(sn)
    nodes.append(helper.make_node("Div", ["s%d" % g, "sn%d" % g], ["sd%d" % g]))
    nodes.append(helper.make_node("Softmax", ["sd%d" % g], ["sm%d" % g], axis=-1))
    nodes.append(helper.make_node("MatMul", ["sm%d" % g, "v%d" % g], ["c%d" % g]))      # [1,64]
    if prev is None:
        prev = "c0"
    else:
        nodes.append(helper.make_node("Add", ["c0", "c1"], ["cc"]))
        prev = "cc"
nodes.append(helper.make_node("Reshape", [prev, "sh4"], ["attn_out"]))  # [1,1,64]
# small output head to make logits-ish output
Wo = init("Wo", (np.random.default_rng(2).standard_normal((64,896))*0.02).astype(np.float32))
nodes.append(helper.make_node("MatMul", ["attn_out", Wo], ["out3"]))   # [1,1,896]

graph = helper.make_graph(
    nodes, "s3b",
    [helper.make_tensor_value_info("x", T.FLOAT, [1,1,896]),
     helper.make_tensor_value_info("kall", T.FLOAT, [2,64,32]),
     helper.make_tensor_value_info("vall", T.FLOAT, [2,32,64])],
    [helper.make_tensor_value_info("out3", T.FLOAT, [1,1,896])],
    inits)
m = helper.make_model(graph, opset_imports=[helper.make_opsetid("", 13)])
m.ir_version = 8
onnx.save(m, "s3b.onnx")
print("s3b.onnx saved, nodes:", len(nodes))
