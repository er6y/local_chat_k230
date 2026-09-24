import nncase, numpy as np, time
rng = np.random.default_rng(3)
co = nncase.CompileOptions()
co.target = 'k230'; co.input_type = 'float32'
ptq = nncase.PTQTensorOptions()
ptq.quant_type = 'int16'; ptq.w_quant_type = 'uint8'
ptq.use_mse_quant_w = False; ptq.calibrate_method = 'NoClip'
ptq.finetune_weights_method = 'NoFineTuneWeights'
feeds = [rng.standard_normal((1,1,896)).astype(np.float32)*0.3]
names = ['input_ids']
import onnx
m = onnx.load('attn0_gqaS3.onnx')
for i in m.graph.input[1:]:
    t = i.type.tensor_type
    dims = [d.dim_value for d in t.shape.dim]
    feeds.append(rng.standard_normal(dims).astype(np.float32)*0.2)
ptq.samples_count = 1
ptq.set_tensor_data([[f] for f in feeds])
c = nncase.Compiler(co)
c.import_onnx(open('attn0_gqaS3.onnx','rb').read(), nncase.ImportOptions())
c.use_ptq(ptq)
t0=time.time(); c.compile()
code = c.gencode_tobytes()
open('attn0_s3_q2cpu.kmodel','wb').write(code)
print('KMODEL %d bytes %.1fs' % (len(code), time.time()-t0))
