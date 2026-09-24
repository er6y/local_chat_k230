#!/usr/bin/env python3
"""gqa_fold_tool.py — GQA 广播消除手术（通用版，吃任意 llm-export ONNX）

问题：llm-export 导出的 GQA 模型用 Expand 节点把 KV 头扩展到 Q 头数，
nncase/k230 编译器把这些 broadcast 实体化成 CPU 拷贝（板上实测占每步 62-71% 时间）。

修法：把每层 attention 改写成严格 2 维、秩完全匹配的矩阵乘：
  Q[1,Q,S,D] 按 KV 头分组 -> [1, r*S, D] @ K_h[D,W] -> [1,r*S,W] -> [1,r,S,W]
  各组 Concat 回原 scores/av 张量（4 维接口不变，下游零改动）。
适用于 q_heads % kv_heads == 0 的任意模型（Qwen2.5 14:2 / ERNIE 8:2 / Qwen3 16:8）。
要求静态形状（新模型先做静态导出）。

用法：
  python3 gqa_fold_tool.py in.onnx out.onnx [--verify]
    --verify  附加 onnxsim 化简 + onnxruntime 随机输入等价对拍
"""
import sys
import numpy as np
import onnx
from onnx import helper, TensorProto


def get_shapes(m):
    inf = onnx.shape_inference.infer_shapes(m)
    shp = {}
    for v in list(inf.graph.value_info) + list(inf.graph.output) + list(inf.graph.input):
        tt = v.type.tensor_type
        if tt.shape.dim:
            shp[v.name] = tuple(d.dim_value if d.dim_value else (d.dim_param or '?') for d in tt.shape.dim)
    return shp


def probe_no_infer(m):
    """shape 推断失败时的轻量结构探测：Expand 计数 + 投影权重维度。"""
    n_expand = sum(1 for n in m.graph.node if n.op_type == 'Expand')
    dims = set()
    for i in m.graph.initializer:
        if i.dims and len(i.dims) == 2 and ('k_proj' in i.name or 'v_proj' in i.name or 'q_proj' in i.name):
            dims.add((i.name.split('/')[0], i.dims[-1]))
    return n_expand, dims


def chain_to_scores(pd, t, depth=8):
    n = pd.get(t)
    h = 0
    while n is not None and n.op_type != 'MatMul':
        n = pd.get(n.input[0])
        h += 1
        if h > depth:
            return None
    return n


def detect(m):
    try:
        shp = get_shapes(m)
    except Exception as e:
        n_expand, dims = probe_no_infer(m)
        print('shape inference failed (%r)' % e)
        print('probe: Expand nodes=%d, proj out-dims=%s' % (n_expand, sorted(dims)[:4]))
        print('RESULT: cannot analyze (external-data graphs need static re-export); '
              'structure probe suggests GQA present' if n_expand else
              'RESULT: no Expand nodes - likely MHA, surgery not needed')
        return 'ABORT'
    pd = {o: n for n in m.graph.node for o in n.output}
    sites = []
    for n in m.graph.node:
        if n.op_type != 'MatMul':
            continue
        av_out = shp.get(n.output[0])
        if not av_out or len(av_out) != 4 or av_out[0] != 1:
            continue
        sm = chain_to_scores(pd, n.input[0])
        if sm is None:
            continue
        sm_out = shp.get(sm.output[0])
        if not sm_out or len(sm_out) != 4 or sm_out[:2] != av_out[:2] or sm_out[2] != av_out[2]:
            continue
        try:
            r5 = pd[sm.input[1]]
            ex = pd[r5.input[0]]
            uk = pd[ex.input[0]]
            if (r5.op_type, ex.op_type, uk.op_type) != ('Reshape', 'Expand', 'Unsqueeze'):
                continue
            kT = shp.get(uk.input[0])
            r7 = pd[n.input[1]]
            ex1 = pd[r7.input[0]]
            uv = pd[ex1.input[0]]
            if (r7.op_type, ex1.op_type, uv.op_type) != ('Reshape', 'Expand', 'Unsqueeze'):
                continue
            vT = shp.get(uv.input[0])
        except KeyError:
            continue
        if not kT or not vT or len(kT) != 4 or kT[0] != 1:
            continue
        Q, S, D = av_out[1], av_out[2], av_out[3]
        W = sm_out[3]
        KV = kT[1]
        if not (isinstance(Q, int) and isinstance(KV, int) and Q > KV >= 1 and Q % KV == 0):
            continue
        if kT[:2] != (1, KV) or vT[:2] != (1, KV) or kT[2] != D or vT[3] != D:
            continue
        sites.append(dict(sm=sm, av=n, q=sm.input[0], kT=uk.input[0], vT=uv.input[0],
                          prob=n.input[0], Q=Q, KV=KV, S=S, W=W, D=D))
    return sites


_uid = [0]


def rsh_node(inp, out, shape):
    _uid[0] += 1
    u = '/gft_rsh_%d' % _uid[0]
    return (helper.make_node('Reshape', [inp, u], [out], name=out + '_n'),
            helper.make_tensor(u, TensorProto.INT64, [len(shape)], list(shape)))


def surgery(m, sites):
    nodes = list(m.graph.node)
    inits = []
    inserts = []
    for idx, st in enumerate(sites):
        r = st['Q'] // st['KV']
        N = '/gft%d_' % idx
        Q, KV, S, W, D = st['Q'], st['KV'], st['S'], st['W'], st['D']
        sm, av = st['sm'], st['av']
        i_sm = next(k for k, n2 in enumerate(nodes) if n2.output[0] == sm.output[0])
        i_av = next(k for k, n2 in enumerate(nodes) if n2.output[0] == av.output[0])
        old_av_out = av.output[0]
        pre = [helper.make_node('Split', [st['q']], [N + 'q%d' % g for g in range(KV)],
                                axis=1, name=N + 'sq')]
        for g in range(KV):
            nn, tt = rsh_node(N + 'q%d' % g, N + 'qr%d' % g, [1, r * S, D])
            pre.append(nn)
            inits.append(tt)
        pre.append(helper.make_node('Split', [st['kT']], [N + 'k%d' % g for g in range(KV)],
                                    axis=1, name=N + 'sk'))
        for g in range(KV):
            nn, tt = rsh_node(N + 'k%d' % g, N + 'kr%d' % g, [D, W])
            pre.append(nn)
            inits.append(tt)
        for g in range(KV):
            pre.append(helper.make_node('MatMul', [N + 'qr%d' % g, N + 'kr%d' % g],
                                        [N + 'sf%d' % g], name=N + 'm%d' % g))
            nn, tt = rsh_node(N + 'sf%d' % g, N + 's%d' % g, [1, r, S, W])
            pre.append(nn)
            inits.append(tt)
        pre.append(helper.make_node('Concat', [N + 's%d' % g for g in range(KV)],
                                    [sm.output[0]], axis=1, name=N + 'cs'))
        sm.output[0] = N + 'sold'
        presplit = [helper.make_node('Split', [st['prob']], [N + 'p%d' % g for g in range(KV)],
                                     axis=1, name=N + 'sp'),
                    helper.make_node('Split', [st['vT']], [N + 'v%d' % g for g in range(KV)],
                                     axis=1, name=N + 'sv')]
        for g in range(KV):
            nn, tt = rsh_node(N + 'p%d' % g, N + 'pr%d' % g, [1, r * S, W])
            presplit.append(nn)
            inits.append(tt)
            nn, tt = rsh_node(N + 'v%d' % g, N + 'vr%d' % g, [W, D])
            presplit.append(nn)
            inits.append(tt)
        av.input[0] = N + 'pr0'
        av.input[1] = N + 'vr0'
        av.output[0] = N + 'a0f'
        post = []
        for g in range(1, KV):
            post.append(helper.make_node('MatMul', [N + 'pr%d' % g, N + 'vr%d' % g],
                                         [N + 'a%df' % g], name=N + 'mav%d' % g))
        for g in range(KV):
            nn, tt = rsh_node(N + 'a%df' % g, N + 'a%d' % g, [1, r, S, D])
            post.append(nn)
            inits.append(tt)
        post.append(helper.make_node('Concat', [N + 'a%d' % g for g in range(KV)],
                                     [old_av_out], axis=1, name=N + 'ca'))
        for nd in reversed(pre):
            inserts.append((i_sm, nd))
        for nd in reversed(post):
            inserts.append((i_av + 1, nd))
        for nd in reversed(presplit):
            inserts.append((i_av, nd))
    inserts.sort(key=lambda t: -t[0])
    for i, nd in inserts:
        nodes.insert(i, nd)
    del m.graph.node[:]
    m.graph.node.extend(nodes)
    m.graph.initializer.extend(inits)
    seen, keep = set(), []
    for i in m.graph.initializer:
        if i.name in seen:
            continue
        seen.add(i.name)
        keep.append(i)
    del m.graph.initializer[:]
    m.graph.initializer.extend(keep)


def verify(src_path, dst_path):
    import onnxruntime as ort
    so = ort.SessionOptions()
    so.intra_op_num_threads = 16
    so.log_severity_level = 3
    a = ort.InferenceSession(src_path, so, providers=['CPUExecutionProvider'])
    b = ort.InferenceSession(dst_path, so, providers=['CPUExecutionProvider'])
    rng = np.random.default_rng(7)
    feeds = {}
    for i in b.get_inputs():
        sh = [d if isinstance(d, int) else 1 for d in i.shape]
        if any(d == 0 for d in sh):
            v = np.zeros(sh, dtype=np.float32)
        elif 'int' in i.type:
            v = np.full(sh, 200, dtype=np.int32)
        else:
            v = (rng.standard_normal(sh) * 0.3).astype(np.float32)
        feeds[i.name] = v
    oa = a.run(None, feeds)
    ob = b.run(None, feeds)
    ok = True
    for k, (va, vb) in enumerate(zip(oa, ob)):
        d = float(np.abs(va - vb).max()) if va.size else 0.0
        am = (int(np.ravel(va).argmax()) == int(np.ravel(vb).argmax())) if va.size else True
        print('  out%d shape=%s maxdiff=%.3g argmax_match=%s' % (k, va.shape, d, am))
        ok = ok and d < 1e-3
    return ok


def main():
    src, dst = sys.argv[1], sys.argv[2]
    do_verify = '--verify' in sys.argv
    m = onnx.load(src)
    sites = detect(m)
    if sites == 'ABORT':
        return 5
    if not sites:
        n_expand = sum(1 for n in m.graph.node if n.op_type == 'Expand')
        if n_expand:
            print('RESULT: %d Expand nodes present but shapes not inferable (dynamic graph) - '
                  'do the STATIC export (fixed seq/window) first, then rerun' % n_expand)
            return 3
        print('RESULT: no GQA Expand pattern found (MHA or already folded) - nothing to do')
        return 0
    sig = sites[0]
    n_layers = len(sites)
    if any(isinstance(x, str) for x in (sig['Q'], sig['KV'], sig['S'], sig['W'], sig['D'])):
        print('detected %d GQA sites but shapes are dynamic: Q=%s KV=%s S=%s W=%s D=%s' % (
            n_layers, sig['Q'], sig['KV'], sig['S'], sig['W'], sig['D']))
        print('RESULT: structure OK - do the STATIC export (fixed seq/window) first, then rerun')
        return 3
    if any(s['Q'] != sig['Q'] or s['KV'] != sig['KV'] or s['D'] != sig['D'] or
           s['S'] != sig['S'] or s['W'] != sig['W'] for s in sites):
        print('RESULT: non-uniform attention shapes across layers - manual handling needed')
        for i, s in enumerate(sites[:4]):
            print('  site%d Q=%s KV=%s S=%s W=%s D=%s' % (i, s['Q'], s['KV'], s['S'], s['W'], s['D']))
        return 2
    print('detected: %d attention sites, Q=%d KV=%d (ratio %d) S=%d W=%d D=%d' % (
        n_layers, sig['Q'], sig['KV'], sig['Q'] // sig['KV'], sig['S'], sig['W'], sig['D']))
    surgery(m, sites)
    onnx.save(m, dst)
    print('surgery done -> %s (nodes %d)' % (dst, len(m.graph.node)))
    if do_verify:
        try:
            import onnxsim
            m2 = onnx.load(dst)
            sim, ok = onnxsim.simplify(m2)
            if ok:
                onnx.save(sim, dst)
                print('onnxsim ok -> %d nodes' % len(sim.graph.node))
        except Exception as e:
            print('onnxsim skipped: %r' % e)
        print('ORT equivalence:')
        ok = verify(src, dst)
        print('RESULT: %s' % ('PASS' if ok else 'FAIL'))
        return 0 if ok else 4
    print('RESULT: OK (no verify requested)')
    return 0


if __name__ == '__main__':
    sys.exit(main())
