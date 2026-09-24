import os, sys, time
import nncase, numpy as np
W = 32
cal = np.load('/ux/work/yilei.wang/k230/calib48_diverse.npz')
x, mask, pos = cal['x'], cal['mask'], cal['pos']
S = x.shape[0]
samples = []
for s in range(S):
    kt_all = np.zeros((48, 64, W), np.float32)
    v_all = np.zeros((48, W, 64), np.float32)
    for l in range(24):
        pk = cal['p%02d' % (2 * l)][s]
        pv = cal['p%02d' % (2 * l + 1)][s]
        for g in range(2):
            kt_all[l * 2 + g] = pk[0, -W:, g, :].T
            v_all[l * 2 + g] = pv[0, -W:, g, :]
    m33 = np.concatenate([mask[s][..., -W:], np.zeros((1, 1, 1, 1), np.float32)], axis=-1)
    samples.append((x[s], m33, pos[s], kt_all, v_all))
co = nncase.CompileOptions()
co.target = 'k230'; co.input_type = 'float32'
ptq = nncase.PTQTensorOptions()
ptq.quant_type = 'int16'; ptq.w_quant_type = 'uint8'
ptq.use_mse_quant_w = False; ptq.calibrate_method = 'NoClip'
ptq.finetune_weights_method = 'NoFineTuneWeights'
ptq.samples_count = S
ptq.set_tensor_data([list(t) for t in zip(*samples)])
t0 = time.time()
c = nncase.Compiler(co)
c.import_onnx(open('/ux/work/yilei.wang/k230/qwen25_24l_s1h256_kv6.onnx', 'rb').read(), nncase.ImportOptions())
print('import %.1fs' % (time.time() - t0), flush=True)
c.use_ptq(ptq)
c.compile(); print('compile %.1fs' % (time.time() - t0), flush=True)
code = c.gencode_tobytes()
open('/ux/work/yilei.wang/k230/llm_kv6_q2cpu.kmodel', 'wb').write(code)
print('DONE %d bytes %.1fs' % (len(code), time.time() - t0), flush=True)
