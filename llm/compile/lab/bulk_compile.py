import nncase
import numpy as np
import time
import sys

tag = sys.argv[1]
rng = np.random.default_rng(3)
co = nncase.CompileOptions()
co.target = 'k230'
co.input_type = 'float32'
ptq = nncase.PTQTensorOptions()
ptq.quant_type = 'int16'
ptq.w_quant_type = 'uint8'
ptq.use_mse_quant_w = False
ptq.calibrate_method = 'NoClip'
ptq.finetune_weights_method = 'NoFineTuneWeights'
feeds = [
    (rng.standard_normal((1, 1, 896)) * 0.3).astype(np.float32),
    np.zeros((1, 1, 1, 33), np.float32),
    np.array([[21]], np.int32),
    (rng.standard_normal((48, 64, 32)) * 0.2).astype(np.float32),
    (rng.standard_normal((48, 32, 64)) * 0.2).astype(np.float32),
]
import onnx
m = onnx.load('s3c_%s.onnx' % tag)
names = [i.name for i in m.graph.input]
print(tag, 'inputs:', names)
full = {'input_ids': feeds[0], 'attention_mask': feeds[1], 'position_ids': feeds[2], 'kt_all': feeds[3], 'v_all': feeds[4]}
sel = [full[n] if n in full else (np.zeros((1, 1, 1, 33), np.float32)) for n in names]
sel = [full[n] for n in names]
ptq.samples_count = 1
ptq.set_tensor_data([[f] for f in sel])
c = nncase.Compiler(co)
c.import_onnx(open('s3c_%s.onnx' % tag, 'rb').read(), nncase.ImportOptions())
c.use_ptq(ptq)
t0 = time.time()
c.compile()
code = c.gencode_tobytes()
open('s3c_%s.kmodel' % tag, 'wb').write(code)
print('KMODEL %d bytes %.1fs' % (len(code), time.time() - t0))
