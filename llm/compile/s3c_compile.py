import nncase
import numpy as np
import time

rng = np.random.default_rng(3)
co = nncase.CompileOptions()
co.target = 'k230'
co.input_type = "float32"
co.dump_ir = True
co.dump_dir = "s3c_dump"
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
ptq.samples_count = 1
ptq.set_tensor_data([[f] for f in feeds])
c = nncase.Compiler(co)
c.import_onnx(open('s3c.onnx', 'rb').read(), nncase.ImportOptions())
c.use_ptq(ptq)
t0 = time.time()
c.compile()
code = c.gencode_tobytes()
open('s3c.kmodel', 'wb').write(code)
print('KMODEL %d bytes %.1fs' % (len(code), time.time() - t0))
