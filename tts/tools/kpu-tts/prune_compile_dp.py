#!/usr/bin/env python3
# prune_compile_dp.py — prune dead nodes from dp_b64_canvas, onnxsim, then
# attempt nncase k230 compile of the static dp graph.
import onnx
from onnxsim import simplify
import collections

SRC = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas.onnx'
PRUNED = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_pruned.onnx'
SIM = '/tmp/vits-icefall-zh-aishell3/dp_b64_canvas_sim.onnx'

m = onnx.load(SRC)

# ---- prune: keep only nodes reaching the graph outputs ----
producers = collections.defaultdict(list)
for n in m.graph.node:
    for o in n.output:
        producers[o].append(n)
needed = set()
stack = [o.name for o in m.graph.output]
while stack:
    t = stack.pop()
    if t in needed:
        continue
    needed.add(t)
    for n in producers.get(t, []):
        for i in n.input:
            stack.append(i)
keep = [n for n in m.graph.node if any(o in needed for o in n.output)]
removed = len(m.graph.node) - len(keep)
del m.graph.node[:]
m.graph.node.extend(keep)
# drop vestigial graph inputs with no consumers (onnxsim leftover)
used_inputs = set()
for n in m.graph.node:
    used_inputs.update(n.input)
real_inputs = [i for i in m.graph.input if i.name in used_inputs]
print('dropping vestigial inputs:', [i.name for i in m.graph.input if i.name not in used_inputs])
del m.graph.input[:]
m.graph.input.extend(real_inputs)
onnx.save(m, PRUNED)
print('pruned: removed', removed, 'dead nodes, kept', len(keep))

c = collections.Counter(n.op_type for n in keep)
print('op mix after prune:', dict(c))
print('NonZero remaining:', c.get('NonZero', 0), ' GatherND:', c.get('GatherND', 0),
      ' ScatterND:', c.get('ScatterND', 0))

import sys
sys.exit(0)

# ---- topo sort (canvas nodes were appended at the end) ----
m2 = onnx.load(PRUNED)
produced = set()
for n in m2.graph.node:
    produced.update(n.output)
produced.update(i.name for i in m2.graph.initializer)
produced.update(i.name for i in m2.graph.input)
sorted_nodes = []
remaining = list(m2.graph.node)
while remaining:
    progressed = False
    for n in list(remaining):
        if all(i in produced for i in n.input):
            sorted_nodes.append(n)
            remaining.remove(n)
            produced.update(n.output)
            progressed = True
            break
    if not progressed:
        print('cycle/unsat at:', remaining[0].name, [i for i in remaining[0].input if i not in produced][:3])
        break
del m2.graph.node[:]
m2.graph.node.extend(sorted_nodes)
print('topo sorted,', len(sorted_nodes), 'nodes')

# ---- onnxsim clean ----
sm, ok = simplify(m2, overwrite_input_shapes={
    '/text_encoder/Split_output_0': [1, 96, 64],
    'tokens': [1, 64], 'tokens_lens': [1], 'speaker': [1],
    'noise_scale_dur': [1], 'alpha': [1]},
    skip_fuse_bn=True)
print('simplify ok:', ok, 'nodes', len(m.graph.node), '->', len(sm.graph.node))
onnx.save(sm, SIM)

c2 = collections.Counter(n.op_type for n in sm.graph.node)
print('op mix after sim:', dict(c2))
print('NonZero:', c2.get('NonZero', 0), 'GatherND:', c2.get('GatherND', 0),
      'ScatterND:', c2.get('ScatterND', 0))
