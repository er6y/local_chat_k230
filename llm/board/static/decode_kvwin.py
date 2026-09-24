# board: kvwin decode (host-maintained KV windows) — speed test with real embeddings
# NOTE: starts with ZERO KV windows (no prefill attach) — measures speed; text is not meaningful
# usage: python3 decode_kvwin.py [first_token] [steps]
import sys
import numpy as np, time
import nncaseruntime as nn

KM = '/mnt/data/static/llm_kvwin.kmodel'
emb_f = '/mnt/data/qwen_official/zm_ours/embeddings_bf16.bin'
EOS = 151645
first_tok = int(sys.argv[1]) if len(sys.argv) > 1 else 99257
STEPS = int(sys.argv[2]) if len(sys.argv) > 2 else 48
plen = 21  # prompt length already in prefill KV

f = open(emb_f, 'rb')
def embed_row(tid):
    f.seek(tid * 896 * 2)
    raw = np.frombuffer(f.read(896 * 2), dtype=np.uint16)
    return (raw.astype(np.uint32) << 16).view(np.float32).reshape(1, 1, 896)

itp = nn.Interpreter()
itp.load_model(open(KM, 'rb').read())

# windows: kt[l,g] = [64,256] (D x pos), vt[l,g] = [256,64] (pos x D); column/row 255 = newest past
ktw = np.zeros((24, 2, 64, 256), np.float32)
vtw = np.zeros((24, 2, 256, 64), np.float32)
ktnew = np.zeros((24, 2, 64), np.float32)
vtnew = np.zeros((24, 2, 64), np.float32)

NEG = np.float32(-10000.0)
LAST_N = 16
PENALTY = 4.0
gen = [first_tok]
tok = first_tok
t0 = time.time()
n = 0
for k in range(STEPS):
    x = embed_row(tok)
    pos = np.array([[plen + k]], dtype=np.int32)
    vis = min(plen + k, 256)          # visible window columns among the 256 past
    m = np.full((1, 1, 1, 257), NEG, np.float32)
    m[0, 0, 0, 256 - vis:] = 0.0      # newest `vis` past + current (in-graph col 256)
    itp.set_input_tensor(0, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(x)))
    itp.set_input_tensor(1, nn.RuntimeTensor.from_numpy(m))
    itp.set_input_tensor(2, nn.RuntimeTensor.from_numpy(pos))
    p = 3
    for l in range(24):
        for g in range(2):
            itp.set_input_tensor(p, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(ktw[l, g]))); p += 1
            itp.set_input_tensor(p, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(vtw[l, g]))); p += 1
    itp.run()
    lg = itp.get_output_tensor(0).to_numpy().reshape(-1).copy()
    for l in range(24):
        for g in range(2):
            o = 1 + l * 4 + g * 2
            ktnew[l, g] = itp.get_output_tensor(o).to_numpy().reshape(64)
            vtnew[l, g] = itp.get_output_tensor(o + 1).to_numpy().reshape(64)
    for t in set(gen[-LAST_N:]):
        lg[t] -= PENALTY
    tok = int(lg.argmax())
    if tok == EOS:
        print('EOS at step %d' % k, flush=True)
        break
    gen.append(tok)
    # roll windows, append new kv
    if k < STEPS - 1:
        ktw[:, :, :, :-1] = ktw[:, :, :, 1:]
        ktw[:, :, :, -1] = ktnew
        vtw[:, :, :-1, :] = vtw[:, :, 1:, :]
        vtw[:, :, -1, :] = vtnew
    n += 1
dt = time.time() - t0
print('steps=%d total=%.3fs per-step=%.1fms tok/s=%.2f' % (n, dt, 1000 * dt / max(n, 1), n / dt))
print('ids:', gen[:24], '...')
print('KVWIN_DECODE_DONE')
