#!/usr/bin/env python3
# find_lr_gather.py — find the Gather that upsamples the encoder hidden
# (LR core), and the encoder output tensor feeding it.
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

# all Gathers whose INDEX producer is downstream of CumSum (duration-derived)
dur_seed = '/CumSum_output_0'
# BFS forward from durations to collect the affected tensor set
affected = set()
front = [dur_seed]
while front:
    t = front.pop()
    if t in affected:
        continue
    affected.add(t)
    for c in cons.get(t, []):
        front.extend(c.output)

gathers = []
for n in G.node:
    if n.op_type == 'Gather' and any(i in affected for i in n.input[1:]):
        data = n.input[0]
        pd = prod.get(data)
        gathers.append((n, data, pd))

print('LR-related Gathers:', len(gathers))
for n, data, pd in gathers[:6]:
    print(f'  Gather {n.name} data={data[:50]} (prod: {pd.op_type if pd else "GRAPH-IN/CONST"} {pd.name if pd else ""})')
    if pd is None:
        # data is initializer or graph input?
        if any(i.name == data for i in G.initializer):
            print('    -> initializer')
    for c in cons.get(n.output[0], [])[:3]:
        print('    consumer:', c.op_type, c.name)

# durations tensor: CumSum's input chain — confirm shape origin
sq = prod['/CumSum_output_0']
print('\ndurations (CumSum input) chain:')
for t in sq.input:
    p = prod.get(t)
    print('  ', t[:40], '<-', (p.op_type + ' ' + p.name) if p else 'leaf')

# what consumes Range_1 (the arange for output positions)
r1 = next((n for n in G.node if n.name == '/Range_1'), None)
if r1:
    print('\nRange_1 consumers:', [(c.op_type, c.name) for c in cons.get('/Range_1_output_0', [])])
# and Slice_1 (used in Sub with Reshape_1)
s1 = next((n for n in G.node if n.name == '/Slice_1'), None)
if s1:
    print('Slice_1 <-', list(s1.input)[:3])
    for c in cons.get('/Slice_1_output_0', [])[:3]:
        print('  consumer:', c.op_type, c.name)
