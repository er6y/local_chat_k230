import os, sys, time
import numpy as np
import nncase
print('nncase:', nncase.__file__, flush=True)
co = nncase.CompileOptions()
co.target = 'k230'
co.input_type = 'float32'
ptq = nncase.PTQTensorOptions()
ptq.quant_type = 'int16'
ptq.w_quant_type = 'uint8'
ptq.use_mse_quant_w = False
ptq.calibrate_method = 'NoClip'
ptq.finetune_weights_method = 'NoFineTuneWeights'
ptq.samples_count = 1
rng = np.random.default_rng(1)
x = (rng.standard_normal((1,1,896)) * 0.5).astype(np.float32)
ptq.set_tensor_data([[x]])
compiler = nncase.Compiler(co)
model_bytes = open('/ux/work/yilei.wang/k230/gemm8k.onnx','rb').read()
compiler.import_onnx(model_bytes, nncase.ImportOptions())
compiler.use_ptq(ptq)
t0=time.time()
compiler.compile()
code = compiler.gencode_tobytes()
open('/ux/work/yilei.wang/k230/gemm8k_srv.kmodel','wb').write(code)
print('KMODEL %d bytes in %.1fs' % (len(code), time.time()-t0))
