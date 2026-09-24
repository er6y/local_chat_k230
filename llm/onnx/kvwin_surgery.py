#!/usr/bin/env python3
# kvwin surgery v5: host-maintained KV windows, no in-graph concat/transpose/split of KV.
# src: qwen25_24l_s1h256_gqaS3.onnx -> dst: qwen25_24l_s1h256_kvwin.onnx
#   inputs:  input_ids, attention_mask, position_ids + per layer 4 windows:
#            ktwin_L{g} [64,256], vwin_L{g} [256,64]   (96 window inputs)
#   outputs: logits + per layer ktnew_L{g}[1,1,1,64], vnew_L{g}[1,1,1,64]  (97 outputs)
# per layer in-graph: sf = concat(MatMul(qr,ktwin)[1,7,256], MatMul(qr,kr_new)[1,7,1])
#                     av = Add(MatMul(pr_past,vwin), MatMul(pr_new,vr_new))
import onnx
from onnx import helper, TensorProto
import numpy as np
from collections import defaultdict
import sys
sys.setrecursionlimit(100000)

SRC = sys.argv[1] if len(sys.argv) > 1 else '/root/k230/input/qwen25_24l_s1h256_gqaS3.onnx'
DST = sys.argv[2] if len(sys.argv) > 2 else '/root/k230/input/qwen25_24l_s1h256_kvwin.onnx'

m = onnx.load(SRC)
g = m.graph
nodes = list(g.node)

prod = {}   # tensor name -> node index
for i, n in enumerate(nodes):
    for o in n.output:
        if o:
            prod[o] = i
cons = defaultdict(list)  # tensor name -> [node indices]
for i, n in enumerate(nodes):
    for x in n.input:
        if x:
            cons[x].append(i)

def shp(name):
    for vi in list(g.input) + list(g.output) + list(g.value_info):
        if vi.name == name:
            return [d.dim_value for d in vi.type.tensor_type.shape.dim]
    return None

NL = 24
new_nodes = []
drop = set()
init_cnt = [0]

def add_init(arr, name=None):
    if name is None:
        init_cnt[0] += 1
        name = '/kvwin/const%d' % init_cnt[0]
    t = helper.make_tensor(name, TensorProto.INT64, arr.shape, arr.astype(np.int64).flatten())
    g.initializer.append(t)
    return name

# ---- per-layer rewiring ----
for L in range(NL):
    pk, pv = 'past_%02dk' % L, 'past_%02dv' % L
    ci_k = cons[pk]  # expect [concat_k, unsqueeze_presents]
    ci_v = cons[pv]
    concat_k = next(i for i in ci_k if nodes[i].op_type == 'Concat')
    concat_v = next(i for i in ci_v if nodes[i].op_type == 'Concat')
    out_k = nodes[concat_k].output[0]   # /Concat_5_output_0 (window 257)
    out_v = nodes[concat_v].output[0]
    new_k = [x for x in nodes[concat_k].input if x != pk][0]   # /Add_1_output_0 (post-RoPE K [1,1,2,64])
    new_v = [x for x in nodes[concat_v].input if x != pv][0]   # /Reshape_3_output_0 [1,1,2,64]

    # K: out_k -> Transpose -> Split -> Reshape x2 -> MatMul.B
    tr_k = next(i for i in cons[out_k] if nodes[i].op_type == 'Transpose')
    trk_out = nodes[tr_k].output[0]
    sp_k = next(i for i in cons[trk_out] if nodes[i].op_type == 'Split')
    k_heads = list(nodes[sp_k].output)          # g0_k0,g0_k1
    krs = []
    for h in k_heads:
        r = next(i for i in cons[h] if nodes[i].op_type == 'Reshape')
        krs.append((r, nodes[r].output[0]))      # (reshape idx, g0_kr0)
    # V: out_v -> Transpose -> Split -> Reshape x2 -> MatMul.B
    tr_v = next(i for i in cons[out_v] if nodes[i].op_type == 'Transpose')
    trv_out = nodes[tr_v].output[0]
    sp_v = next(i for i in cons[trv_out] if nodes[i].op_type == 'Split')
    v_heads = list(nodes[sp_v].output)
    vrs = []
    for h in v_heads:
        r = next(i for i in cons[h] if nodes[i].op_type == 'Reshape')
        vrs.append((r, nodes[r].output[0]))

    # drop the KV plumbing nodes (concat, transposes, splits, reshapes, presents unsqueeze/concat)
    drop.update([concat_k, concat_v, tr_k, tr_v, sp_k, sp_v])
    drop.update(r for r, _ in krs)
    drop.update(r for r, _ in vrs)
    for i in cons[out_k]:
        if nodes[i].op_type == 'Unsqueeze':
            drop.add(i)
    for i in cons[out_v]:
        if nodes[i].op_type == 'Unsqueeze':
            drop.add(i)
    # presents concat + full downstream closure (KV window feeds nothing else)
    for i in list(cons[out_k]) + list(cons[out_v]):
        if nodes[i].op_type == 'Unsqueeze':
            for c in cons[nodes[i].output[0]]:
                if nodes[c].op_type == 'Concat':
                    drop.add(c)
                    stack = [nodes[c].output[0]]
                    while stack:
                        t = stack.pop()
                        for d in cons[t]:
                            if d not in drop:
                                drop.add(d)
                                stack.extend(nodes[d].output)

    # new K/V slices -> outputs
    s0 = add_init(np.array([0])); s1 = add_init(np.array([1])); s2 = add_init(np.array([2]))
    e1 = add_init(np.array([1])); e2 = add_init(np.array([2]))
    ax2 = add_init(np.array([2]))
    kt0, kt1 = 'ktnew_%02d_0' % L, 'ktnew_%02d_1' % L
    vt0, vt1 = 'vtnew_%02d_0' % L, 'vtnew_%02d_1' % L
    new_nodes.append(helper.make_node('Slice', [new_k, s0, e1, ax2], [kt0], name='kw_%d_a' % L))
    new_nodes.append(helper.make_node('Slice', [new_k, s1, e2, ax2], [kt1], name='kw_%d_b' % L))
    new_nodes.append(helper.make_node('Slice', [new_v, s0, e1, ax2], [vt0], name='kw_%d_c' % L))
    new_nodes.append(helper.make_node('Slice', [new_v, s1, e2, ax2], [vt1], name='kw_%d_d' % L))

    # rewire matmuls: scores
    for gi in range(2):
        mm = next(i for i in cons[krs[gi][1]] if nodes[i].op_type == 'MatMul')
        qr = nodes[mm].input[0]
        # past part: MatMul(qr, ktwin) -> [1,7,256]
        sf_p = 'kwsf_%02d_%d_p' % (L, gi)
        new_nodes.append(helper.make_node('MatMul', [qr, 'ktwin_%02d_%d' % (L, gi)], [sf_p], name='kw_sfp_%d_%d' % (L, gi)))
        # new part: kr_new [64,1]
        rsh = add_init(np.array([64, 1], np.int64), name='/kvwin/krsh_%d_%d' % (L, gi))
        krn = 'kwkrn_%02d_%d' % (L, gi)
        new_nodes.append(helper.make_node('Reshape', [kt0 if gi == 0 else kt1, rsh], [krn], name='kw_krn_%d_%d' % (L, gi)))
        sf_n = 'kwsf_%02d_%d_n' % (L, gi)
        new_nodes.append(helper.make_node('MatMul', [qr, krn], [sf_n], name='kw_sfn_%d_%d' % (L, gi)))
        # concat -> feed the downstream Reshape (old sf reshape node input)
        sfc = 'kwsf_%02d_%d' % (L, gi)
        new_nodes.append(helper.make_node('Concat', [sf_p, sf_n], [sfc], axis=2, name='kw_sfc_%d_%d' % (L, gi)))
        # old sf reshape: MatMul -> Reshape(srsh) -> s_g ; redirect reshape input to sfc
        old_sf = nodes[mm].output[0]
        for r in cons[old_sf]:
            if nodes[r].op_type == 'Reshape':
                for j, x in enumerate(nodes[r].input):
                    if x == old_sf:
                        nodes[r].input[j] = sfc
        drop.add(mm)

    for gi in range(2):
        mm = next(i for i in cons[vrs[gi][1]] if nodes[i].op_type == 'MatMul')
        pr = nodes[mm].input[0]   # [1,7,257]
        psh = shp(pr)
        assert psh == [1, 7, 257], (pr, psh)
        cst = add_init(np.array([0])); cen = add_init(np.array([256])); cax = add_init(np.array([2]))
        pr_p = 'kwpr_%02d_%d_p' % (L, gi)
        new_nodes.append(helper.make_node('Slice', [pr, cst, cen, cax], [pr_p], name='kw_prp_%d_%d' % (L, gi)))
        cst2 = add_init(np.array([256])); cen2 = add_init(np.array([257]))
        pr_n = 'kwpr_%02d_%d_n' % (L, gi)
        new_nodes.append(helper.make_node('Slice', [pr, cst2, cen2, cax], [pr_n], name='kw_prn_%d_%d' % (L, gi)))
        av_p = 'kwav_%02d_%d_p' % (L, gi)
        new_nodes.append(helper.make_node('MatMul', [pr_p, 'vwin_%02d_%d' % (L, gi)], [av_p], name='kw_avp_%d_%d' % (L, gi)))
        rsh2 = add_init(np.array([1, 64], np.int64), name='/kvwin/vrsh_%d_%d' % (L, gi))
        vrn = 'kwvrn_%02d_%d' % (L, gi)
        new_nodes.append(helper.make_node('Reshape', [vt0 if gi == 0 else vt1, rsh2], [vrn], name='kw_vrn_%d_%d' % (L, gi)))
        av_n = 'kwav_%02d_%d_n' % (L, gi)
        new_nodes.append(helper.make_node('MatMul', [pr_n, vrn], [av_n], name='kw_avn_%d_%d' % (L, gi)))
        avc = 'kwav_%02d_%d' % (L, gi)
        new_nodes.append(helper.make_node('Add', [av_p, av_n], [avc], name='kw_avc_%d_%d' % (L, gi)))
        old_av = nodes[mm].output[0]
        for r in cons[old_av]:
            if nodes[r].op_type == 'Reshape':
                for j, x in enumerate(nodes[r].input):
                    if x == old_av:
                        nodes[r].input[j] = avc
        drop.add(mm)

# ---- apply ----
kept = [n for i, n in enumerate(nodes) if i not in drop]
# remove presents output
g.output.remove(next(o for o in g.output if o.name == 'presents'))
# remove old past inputs, add window inputs
ins = [vi for vi in g.input if not vi.name.startswith('past_')]
del g.input[:]
g.input.extend(ins)
for L in range(NL):
    for gi in range(2):
        g.input.append(helper.make_tensor_value_info('ktwin_%02d_%d' % (L, gi), TensorProto.FLOAT, [64, 256]))
        g.input.append(helper.make_tensor_value_info('vwin_%02d_%d' % (L, gi), TensorProto.FLOAT, [256, 64]))
for L in range(NL):
    for nm in ('ktnew_%02d_0' % L, 'ktnew_%02d_1' % L, 'vtnew_%02d_0' % L, 'vtnew_%02d_1' % L):
        g.output.append(helper.make_tensor_value_info(nm, TensorProto.FLOAT, [1, 1, 1, 64]))

del g.node[:]
g.node.extend(kept)
g.node.extend(new_nodes)

# stable topological sort (appended nodes feed mid-graph reshapes)
nodes2 = list(g.node)
prod2 = {}
for i, n in enumerate(nodes2):
    for o in n.output:
        if o:
            prod2[o] = i
pos = {id(n): j for j, n in enumerate(nodes2)}
done = [False] * len(nodes2)
order = []
def visit(n):
    j = pos[id(n)]
    if done[j]:
        return
    for x in n.input:
        if x in prod2:
            visit(nodes2[prod2[x]])
    done[j] = True
    order.append(n)
graph_inputs = {vi.name for vi in g.input}
init_names = {init.name for init in g.initializer}
for n in nodes2:
    visit(n)
del g.node[:]
g.node.extend(order)

onnx.checker.check_model(m)
print('KVWIN nodes=%d inputs=%d outputs=%d drops=%d' % (len(g.node), len(g.input), len(g.output), len(drop)))
onnx.save(m, DST)
print('SAVED', DST)
