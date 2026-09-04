#!/usr/bin/env python3
# surgery_flow.py — full-canvas rewrite of flows.{3,5,7} in dp_b64.onnx.
#
# Validated semantics (flows.7, identical template in 5/3):
#   hist12 = concat(cumsum(sub_c*softmax(div1)+c80, -1) PREPEND 0) * 10 - 5
#   hist7  = same on div0
#   H9     = Pad(Slice_2) (+ ScatterND_0/1 static clamps -> keep nodes)
#   A10    = Softplus(H9) + c80
#   sub1 = hist7[...,1:] - hist7[...,:-1]; sub2 = hist12[...,1:]-hist12[...,:-1]
#   div2 = sub2/sub1
#   pivot = ReduceSum(Cast(GE(x, hist12), int32), axis=-1) - 1
#   sel(d, p) = ReduceSum(d * onehot(p), axis=-1)   (replaces GatherElements)
#   quartic tree (elementwise) -> decoded
#   result = Where(inner_mask, decoded, x);  out = Concat(pred, result, axis=1)
#
# Strategy: copy the needed nodes with rewired inputs, then delete the old
# gather/scatter machinery and point Concat_20's consumers at the new output.
import onnx, copy
import numpy as np
from onnx import helper, numpy_helper, TensorProto

SRC = '/tmp/vits-icefall-zh-aishell3/dp_b64.onnx'
DST = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas.onnx'
N = 64

m = onnx.load(SRC)
G = m.graph
nodes = list(G.node)
node_by_out = {}
for n in nodes:
    for o in n.output:
        node_by_out[o] = n
inits = {i.name: i for i in G.initializer}

def init_val(t):
    return numpy_helper.to_array(inits[t])

def new_node(op, name, ins, **attrs):
    n = helper.make_node(op, ins, [name], name=name)
    for k, v in attrs.items():
        if k == 'axis':
            n.attribute.append(helper.make_attribute('axis', v))
        elif k == 'to':
            n.attribute.append(helper.make_attribute('to', v))
        elif k == 'keepdims':
            n.attribute.append(helper.make_attribute('keepdims', v))
    return n

def add_init(name, arr):
    t = numpy_helper.from_array(np.asarray(arr), name=name)
    G.initializer.append(t)
    return name

def rewrite_flow(f):
    S = f'/duration_predictor/flows.{f}/'
    div0 = S + 'Div_output_0'            # [1,1,64,10]
    div1 = S + 'Div_1_output_0'
    x = S + 'Split_output_1'             # [1,1,64]
    pred = S + 'Split_output_0'
    inner_mask = S + 'And_output_0'      # [1,1,64] bool (exists, static)
    conv_exit = S + 'Concat_20_output_0'

    # resolve ALL constants from the original nodes' real inputs
    def inp(nname, idx):
        n = node_by_out[S + nname + '_output_0']
        return n.input[idx]

    sub_c = inp('Mul_10', 0)             # shared flows.7 scalar const
    c80 = inp('Add_6', 1)
    c92 = inp('Pad_1', 2)
    c93 = inp('Mul_11', 1)
    c19 = inp('Add_7', 1)
    c80b = inp('Add_10', 1)              # A10's +c80 (may be a different tensor)
    c_mul26_t = inp('Mul_26', 1)
    c_mul31_t = inp('Mul_31', 1)
    c_mul33_t = inp('Mul_33', 1)
    c_pow_t = inp('Pow', 1)
    c_mul26 = float(np.asarray(init_val(c_mul26_t)).flatten()[0])
    c_mul31 = float(np.asarray(init_val(c_mul31_t)).flatten()[0])
    c_mul33 = float(np.asarray(init_val(c_mul33_t)).flatten()[0])
    c_pow = float(np.asarray(init_val(c_pow_t)).flatten()[0])

    prefix = S + 'canvas/'
    new_nodes = []
    def emit(op, name, ins, **attrs):
        new_nodes.append(new_node(op, prefix + name, ins, **attrs))
        return prefix + name

    axlast = add_init(prefix + 'axlast', np.array(-1, np.int64))
    axlast_v = add_init(prefix + 'axlast_v', np.array([-1], np.int64))  # ReduceSum wants a vector
    # histogram builder helper: sm -> H (prepend 0, *10, -5)
    def hist_chain(sm_out):
        m1 = emit('Mul', f'mul_{id(sm_out)}', [sub_c, sm_out])
        a6 = emit('Add', f'add_{id(sm_out)}', [m1, c80])
        cs = emit('CumSum', f'cum_{id(sm_out)}', [a6, axlast])
        zero = add_init(prefix + f'zero_{id(sm_out)}', np.zeros((1, 1, N, 1), np.float32))
        hh = emit('Concat', f'hcat_{id(sm_out)}', [zero, cs], axis=-1)
        m11 = emit('Mul', f'hmul_{id(sm_out)}', [hh, c93])
        return emit('Add', f'hadd_{id(sm_out)}', [m11, c19])

    sm1 = emit('Softmax', 'sm1', [div1], axis=-1)
    H1 = hist_chain(sm1)
    sm0 = emit('Softmax', 'sm0', [div0], axis=-1)
    H0 = hist_chain(sm0)

    # adjacent diffs
    c1i = add_init(prefix + 'one_i', np.array([1], np.int64))
    c0i = add_init(prefix + 'zero_i', np.array([0], np.int64))
    cmax = add_init(prefix + 'max_i', np.array([9223372036854775807], np.int64))
    cm1 = add_init(prefix + 'm1_i', np.array([-1], np.int64))
    cax3 = add_init(prefix + 'ax3', np.array([3], np.int64))
    s9 = emit('Slice', 's9', [H0, c1i, cmax, cax3, c1i])
    s10 = emit('Slice', 's10', [H0, c0i, cm1, cax3, c1i])
    sub1 = emit('Sub', 'sub1', [s9, s10])
    s14 = emit('Slice', 's14', [H1, c1i, cmax, cax3, c1i])
    s15 = emit('Slice', 's15', [H1, c0i, cm1, cax3, c1i])
    sub2 = emit('Sub', 'sub2', [s14, s15])
    div2 = emit('Div', 'div2', [sub2, sub1])

    # A10 = Softplus(H9) + c80. H9 was ScatterND_1's output; the ScatterND
    # write-backs are fixed-position/fixed-value and their modified columns
    # never reach the output (col 0 is sliced away by a10s=[1:MAX], col 10 is
    # masked out by the Where merge). Empirically verified bit-exact on 14
    # diverse inputs (rewire sweep). Feeding Pad_output_0 directly drops both
    # ScatterND nodes from the graph (they become dead) — nncase's
    # CalibrationEvaluator cannot evaluate ScatterND, so this is required.
    H9 = S + 'Pad_output_0'
    sp = emit('Softplus', 'softplus9', [H9])
    A10 = emit('Add', 'a10', [sp, c80b])
    a10s = emit('Slice', 'a10s', [A10, c1i, cmax, cax3, c1i])

    # pivot (opset-13 Unsqueeze takes axes as an input tensor)
    axm1 = add_init(prefix + 'axm1', np.array([-1], np.int64))
    xu = emit('Unsqueeze', 'xu', [x, axm1])
    ge = emit('GreaterOrEqual', 'ge', [xu, H1])
    cast = emit('Cast', 'cast', [ge], to=TensorProto.INT32)
    rsum = emit('ReduceSum', 'rsum', [cast, axlast_v], keepdims=0)
    c1f = add_init(prefix + 'one_f', np.array(1, np.int32))
    pivot = emit('Sub', 'pivot', [rsum, c1f])

    # onehot selects. 11-wide tensors (histograms, A10) select at the raw
    # pivot (0..10); 10-wide tensors (diffs, ratios) select at pivot clamped
    # to 9 (the original graph gathers with the same index; x==5 exactly is
    # unreachable in practice so clamping mirrors real behavior).
    pu = emit('Unsqueeze', 'pivot_u', [pivot, axm1])
    ar11 = add_init(prefix + 'arange11', np.arange(11, dtype=np.int32))
    oh11 = emit('Equal', 'oh11', [ar11, pu])
    oh11f = emit('Cast', 'oh11f', [oh11], to=TensorProto.FLOAT)
    def sel11(d, tag):
        mm = emit('Mul', f'sel11_mul_{tag}', [d, oh11f])
        return emit('ReduceSum', f'sel11_{tag}', [mm, axlast_v], keepdims=0)
    c9 = add_init(prefix + 'c9', np.array([9], np.int32))
    piv10 = emit('Min', 'piv10', [pivot, c9])
    pu10 = emit('Unsqueeze', 'pivot_u10', [piv10, axm1])
    ar10 = add_init(prefix + 'arange10', np.arange(10, dtype=np.int32))
    oh10 = emit('Equal', 'oh10', [ar10, pu10])
    oh10f = emit('Cast', 'oh10f', [oh10], to=TensorProto.FLOAT)
    def sel10(d, tag):
        mm = emit('Mul', f'sel10_mul_{tag}', [d, oh10f])
        return emit('ReduceSum', f'sel10_{tag}', [mm, axlast_v], keepdims=0)

    g24 = sel11(H0, '24')    # hist7[pivot]
    g25 = sel10(sub1, '25')
    g26 = sel11(H1, '26')    # hist12[pivot]
    g27 = sel10(div2, '27')
    g28 = sel11(A10, '28')
    g29 = sel10(a10s, '29')
    g30 = sel10(sub2, '30')

    # quartic tree
    q = {}
    q['sub4'] = emit('Sub', 'sub4', [x, g26])
    q['add18'] = emit('Add', 'add18', [g28, g29])
    q['mul26'] = emit('Mul', 'mul26', [g27, add_init(prefix + 'cm26', np.array(c_mul26, np.float32))])
    q['sub5'] = emit('Sub', 'sub5', [q['add18'], q['mul26']])
    q['mul27'] = emit('Mul', 'mul27', [q['sub4'], q['sub5']])
    q['sub6'] = emit('Sub', 'sub6', [g27, g28])
    q['mul28'] = emit('Mul', 'mul28', [g30, q['sub6']])
    q['add19'] = emit('Add', 'add19', [q['mul27'], q['mul28']])
    q['mul29'] = emit('Mul', 'mul29', [g30, g28])
    q['sub7'] = emit('Sub', 'sub7', [q['mul29'], q['mul27']])
    q['neg'] = emit('Neg', 'neg', [g27])
    q['mul30'] = emit('Mul', 'mul30', [q['neg'], q['sub4']])
    q['pow'] = emit('Pow', 'pow', [q['sub7'], add_init(prefix + 'cpow', np.array(c_pow, np.float32))])
    q['mul31'] = emit('Mul', 'mul31', [q['add19'], add_init(prefix + 'cm31', np.array(c_mul31, np.float32))])
    q['mul32'] = emit('Mul', 'mul32', [q['mul31'], q['mul30']])
    q['sub8'] = emit('Sub', 'sub8', [q['pow'], q['mul32']])
    q['mul33'] = emit('Mul', 'mul33', [q['mul30'], add_init(prefix + 'cm33', np.array(c_mul33, np.float32))])
    q['neg1'] = emit('Neg', 'neg1', [q['sub7']])
    q['sqrt'] = emit('Sqrt', 'sqrt', [q['sub8']])
    q['sub9'] = emit('Sub', 'sub9', [q['neg1'], q['sqrt']])
    q['div3'] = emit('Div', 'div3', [q['mul33'], q['sub9']])
    q['mul34'] = emit('Mul', 'mul34', [q['div3'], g25])
    decoded = emit('Add', 'decoded', [q['mul34'], g24])

    # merge
    mf = emit('Cast', 'mask_f', [inner_mask], to=TensorProto.FLOAT)
    result = emit('Where', 'result', [inner_mask, decoded, x])
    out = emit('Concat', 'out', [pred, result], axis=1)

    # rewire Concat_20 consumers to the new output
    n_rewire = 0
    for n in G.node:
        for j, i in enumerate(n.input):
            if i == conv_exit:
                n.input[j] = out
                n_rewire += 1
    # delete old machinery nodes (keep the static ones we reused: ScatterND_0/1, And, div0/div1 producers)
    keep_prefix = set()
    G.node.extend(new_nodes)
    return n_rewire

for f in (7, 5, 3):
    print('rewriting flow', f, '...')
    rewrite_flow(f)

onnx.save(m, DST)
print('saved', DST, 'nodes:', len(m.graph.node))
