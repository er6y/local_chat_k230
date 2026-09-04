#!/usr/bin/env python3
# mk_dp_only_k230.py — compile the dp-only graph (dp_x entry, enc excluded)
# to a k230 kmodel. Calibration: 20 real sentences, zero-padded dp_x.
import os
import glob
import numpy as np
import onnx
import nncase

SRC = '/tmp/vits-icefall-zh-aishell3/dp_only.onnx'
OUT = '/tmp/kpu_poc/dp_k230_only'
CALIB = '/tmp/kpu_poc/dpx_calib'
os.makedirs(OUT, exist_ok=True)

# topo-sort defensively (graph is already sorted, cheap no-op check)
m0 = onnx.load(SRC)
produced = set(i.name for i in m0.graph.initializer)
produced.update(i.name for i in m0.graph.input)
sorted_nodes = []
remaining = list(m0.graph.node)
while remaining:
    progressed = False
    for n in list(remaining):
        if all(i in produced or i == '' for i in n.input):
            sorted_nodes.append(n)
            remaining.remove(n)
            produced.update(n.output)
            progressed = True
            break
    if not progressed:
        raise RuntimeError('unsatisfiable node: ' + remaining[0].name)
assert len(sorted_nodes) == len(m0.graph.node), 'topo sort changed node count?'
print('graph already topologically sorted:', len(sorted_nodes), 'nodes')

# calibration: param-major over the 20 real sentences
xs = sorted(glob.glob(CALIB + '/x_*.npy'))
tks = sorted(glob.glob(CALIB + '/tok_*.npy'))
tls = sorted(glob.glob(CALIB + '/tl_*.npy'))
sps = sorted(glob.glob(CALIB + '/spk_*.npy'))
assert len(xs) == len(tks) == len(tls) == len(sps) and len(xs) > 0
print('calibration samples:', len(xs))
samples = [[np.load(p) for p in xs],
           [np.load(p).astype(np.int64) for p in tks],
           [np.load(p).astype(np.int64) for p in tls],
           [np.load(p).astype(np.int64) for p in sps],
           [np.array([0.8], np.float32) for _ in xs],
           [np.array([1.0], np.float32) for _ in xs]]
n_samples = len(xs)
# set_tensor_data transposes (param-major) and the C# provider re-chunks
# sample-major — pass param-major, count = per-param sample count.

co = nncase.CompileOptions()
co.target = 'k230'
co.quant_type = 'int8'
co.w_quant_type = 'int8'
co.calibrate_method = 'NoClip'
co.dump_ir = False   # DumpTraceInfoPass crashes on Erf in 2.11
co.dump_asm = False

compiler = nncase.Compiler(co)
import_options = nncase.ImportOptions()
with open(SRC, 'rb') as f:
    compiler.import_onnx(f.read(), import_options)

ptq = nncase.PTQTensorOptions()
ptq.calibrate_method = 'NoClip'
ptq.quant_type = 'int8'
ptq.w_quant_type = 'int8'
ptq.export_weight_range_by_channel = True
ptq.samples_count = n_samples
ptq.set_tensor_data(samples)
compiler.use_ptq(ptq)
compiler.compile()

with open(os.path.join(OUT, 'dp_only.kmodel'), 'wb') as f:
    compiler.gencode(f)
print('KMODEL ->', os.path.join(OUT, 'dp_only.kmodel'))
