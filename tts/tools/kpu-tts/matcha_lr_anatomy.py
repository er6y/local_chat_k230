#!/usr/bin/env python3
# matcha_lr_anatomy.py — precise anatomy of the length regulator + decoder entry:
#   which tensor gets upsampled, exact ops from durations to decoder input.
import onnx

m = onnx.load('/tmp/matcha-icefall-zh-baker/model-steps-3.onnx')
G = m.graph
prod = {}
for n in G.node:
    for o in n.output:
        prod[o] = n
cons = {}
for n in G.node:
    for i in n.input:
        cons.setdefault(i, []).append(n)

# 1) the CumSum -> forward: who consumes CumSum_output_0?
cs = next(n for n in G.node if n.op_type == 'CumSum')
print('CumSum ->', [(c.op_type, c.name) for c in cons.get(cs.output[0], [])])

# 2) walk the LR cluster forward 8 hops printing everything
frontier = [cs.output[0]]
seen = set()
for depth in range(6):
    nxt = []
    for t in frontier:
        for c in cons.get(t, []):
            key = c.name or c.op_type
            if key in seen:
                continue
            seen.add(key)
            print('  ' * depth, f'{c.op_type} {c.name} <- {list(c.input)[:3]} -> {list(c.output)[:2]}')
            nxt.extend(c.output)
    frontier = nxt
    if not frontier:
        break

# 3) decoder entry: /Cast_3 producer chain backward
print('\n/Cast_3 backward chain:')
cur = '/Cast_3_output_0'
for depth in range(10):
    n = prod.get(cur)
    if n is None:
        print('  ' * depth, 'LEAF:', cur)
        break
    print('  ' * depth, f'{n.op_type} {n.name} <- {list(n.input)[:3]}')
    cur = n.input[0]

# 4) Reshape_1's data input (the upsampled gather result?)
r1 = next((n for n in G.node if n.name == '/Reshape_1'), None)
if r1 is not None:
    print('\nReshape_1 inputs:', list(r1.input))
    p = prod.get(r1.input[0])
    if p:
        print('  data producer:', p.op_type, p.name, '->', list(p.output))

# 5) what feeds the Gather (if any) that produces upsampled hidden
print('\nGathers consuming Range_1 or mask tensors:')
for n in G.node:
    if n.op_type == 'Gather':
        for i in n.input[1:]:
            p = prod.get(i)
            if p is not None and p.name in ('/Range_1', '/Range', '/Unsqueeze_5', '/Unsqueeze_6'):
                print(f'  Gather {n.name} data={n.input[0][:40]} idx_from={p.name} -> {list(n.output)[:1]}')
                pd = prod.get(n.input[0])
                print('    data producer:', pd.op_type if pd else '?', pd.name if pd else n.input[0][:30])
