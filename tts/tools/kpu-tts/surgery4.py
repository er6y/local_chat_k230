#!/usr/bin/env python3
# surgery4.py — final split: C = backward(C_OUT) stopping at matmul AND the
# A-side (encoder+DP) family; dangling tensors become C inputs (mm_out + mask).
import numpy as np
import onnx
import onnxruntime as ort
from onnxsim import simplify

SRC = '/tmp/matcha-icefall-zh-baker/model-steps-3.onnx'
DUR = '/Squeeze_output_0'
HID = '/encoder/Mul_1_output_0'
XB = 256
MB = 512

m = onnx.load(SRC)
G = m.graph
prod = {}
for n in G.node:
    for o in n.output:
        prod[o] = n
matmul = next(n for n in G.node if n.op_type == 'MatMul' and '/Transpose_1_output_0' in n.input)
C_OUT = next(n for n in G.node if n.name == '/Slice_2').input[0]

# A-side nodes: backward from DUR/HID
sideA = set()
stack = [DUR, HID]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None or n.name in sideA:
        continue
    sideA.add(n.name)
    stack.extend(n.input)
print('A-side nodes:', len(sideA))

# C-side: backward from C_OUT, stopping at matmul or A-side
inC = set()
dangling = set()
stack = [C_OUT]
while stack:
    t = stack.pop()
    n = prod.get(t)
    if n is None:
        continue
    if n.name == matmul.name or n.name in sideA:
        dangling.add(t)
        continue
    if n.name in inC:
        continue
    inC.add(n.name)
    stack.extend(n.input)
print('C nodes:', len(inC), 'dangling inputs:', dangling)

# original scalars used by C
used = set()
for name in inC:
    for i in next(n for n in G.node if n.name == name).input:
        used.add(i)

mC = onnx.load(SRC)
keepC = [n for n in mC.graph.node if n.name in inC]
outs_internal = {o for n in keepC for o in n.output}
init_names = {i.name for i in mC.graph.initializer}
initsC = [i for i in mC.graph.initializer
          if i.name in {x for n in keepC for x in n.input}]
del mC.graph.node[:]
del mC.graph.initializer[:]
del mC.graph.output[:]
del mC.graph.input[:]
mC.graph.node.extend(keepC)
mC.graph.initializer.extend(initsC)
for i in onnx.load(SRC).graph.input:
    if i.name in used and i.name not in dangling:
        mC.graph.input.append(i)
for d in sorted(dangling):
    shape = [1, MB, 80] if d == matmul.output[0] else [1, 1, XB]
    mC.graph.input.append(onnx.helper.make_tensor_value_info(
        d, onnx.TensorProto.FLOAT, shape))
mC.graph.output.append(onnx.helper.make_tensor_value_info(
    C_OUT, onnx.TensorProto.FLOAT, [1, 80, MB]))

# topo
produced = set(i.name for i in mC.graph.initializer)
produced.update(i.name for i in mC.graph.input)
rem = list(mC.graph.node)
srt = []
while rem:
    for n in list(rem):
        if all(i in produced or i == '' for i in n.input):
            srt.append(n)
            rem.remove(n)
            produced.update(n.output)
            break
    else:
        raise RuntimeError('unsat: ' + rem[0].name)
del mC.graph.node[:]
mC.graph.node.extend(srt)

# pin shapes: mm_out [1,MB,80]; mask dangles [1,XB,MB]; scalars [1]
pin = {matmul.output[0]: [1, MB, 80]}
for d in dangling:
    if d != matmul.output[0]:
        pin[d] = [1, 1, XB]
sm, ok = simplify(mC, overwrite_input_shapes=pin, skip_fuse_bn=True)
print('C sim ok:', ok, 'nodes:', len(srt), '->', len(sm.graph.node))
onnx.save(sm, '/tmp/matcha-icefall-zh-baker/matcha_C512.onnx')
print('C inputs:', [(i.name, i.shape) for i in
                    ort.InferenceSession('/tmp/matcha-icefall-zh-baker/matcha_C512.onnx',
                                         providers=['CPUExecutionProvider']).get_inputs()])

# save A256 as well (from surgery3 output — regenerate identically here if needed)
import os
if not os.path.exists('/tmp/matcha-icefall-zh-baker/matcha_A256.onnx'):
    raise SystemExit('run surgery3 first for Part A')
print('done')
