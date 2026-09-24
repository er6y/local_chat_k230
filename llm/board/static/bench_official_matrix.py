# bench_official_matrix.py — 官方模型 decode 条件扫（找 ~202ms/4.9tok/s 的复现条件）
# 背景：官方宣称 4.8-4.9 tok/s（~202ms/步）。我们同板实测：python harness H=29 → 290ms
# （带 poke），qwen_chat 自报 335ms。本脚本扫 H（KV 深度）× 序列长，定位差距来源。
# 前置：golden window（reboot→230s）+ drop_caches + noc_poke 已由调用方完成。
# 用法：python3 bench_official_matrix.py [H列表逗号分隔，默认 29,63,127,255]
# 注意：一个进程只装一次模型（CMA 协议：装载之间必须 reboot），所以每次调用只吃一个 H。
# 用法：python3 bench_official_matrix.py <H>   （调用方负责 reboot+golden+poke 循环）
import sys, time
import numpy as np
import nncaseruntime as nn

KM = '/mnt/data/qwen_official/Qwen2.5-0.5B-Instruct/llm.kmodel'
HS = [int(h) for h in (sys.argv[1] if len(sys.argv) > 1 else '29,63,127,255').split(',')]
REPS = 8
rng = np.random.default_rng(7)

def feed(H, seq=1):
    x = (rng.standard_normal((1, seq, 896)) * 0.3).astype(np.float32)
    if seq == 1:
        m = np.zeros((1, 1, 1, 1), np.float32)   # kv_seq_len forced to seq_len
        pos = np.array([[H]], np.int32)
    else:
        m = np.zeros((1, 1, seq, H + seq), np.float32)
        m[..., H:] = np.triu(np.full((seq, seq), -10000.0, np.float32), 1)
        pos = np.arange(H, H + seq, dtype=np.int32).reshape(1, seq)
    past = (rng.standard_normal((24, 2, 1, H, 2, 64)) * 0.2).astype(np.float32)
    return [x, m, pos, past]

print('H(seq=1 decode) | mean ms | min ms | tok/s')
print('-' * 44)
for H in HS:
    try:
        itp = nn.Interpreter()
        itp.load_model(open(KM, 'rb').read())
        ts_feed = feed(H)
        for i, t in enumerate(ts_feed):
            itp.set_input_tensor(i, nn.RuntimeTensor.from_numpy(np.ascontiguousarray(t)))
        itp.run(); itp.run()      # warm
        ts = []
        for _ in range(REPS):
            t0 = time.time(); itp.run(); ts.append((time.time() - t0) * 1000)
        ts = ts[2:]
        mean = sum(ts) / len(ts)
        print('%4d            | %7.1f | %6.1f | %5.2f' % (H, mean, min(ts), 1000.0 / mean), flush=True)
    except Exception as e:
        print('%4d            | FAIL %s' % (H, repr(e)[:60]), flush=True)

print('OFFMAT_DONE', flush=True)
