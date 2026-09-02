# -*- coding: utf-8 -*-
"""PC 基线：transformers 跑 Qwen3-0.6B，记录 golden 输出 + 分段耗时"""
import json, time, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
import torch
from transformers import AutoModelForCausalLM, AutoTokenizer

MODEL_DIR = r"d:\yilei.wang\k230_prj\k230_llm\models\qwen3_06b"
OUT = r"d:\yilei.wang\k230_prj\k230_llm\models\baseline_golden.json"

t0 = time.perf_counter()
tok = AutoTokenizer.from_pretrained(MODEL_DIR)
model = AutoModelForCausalLM.from_pretrained(MODEL_DIR, dtype=torch.float32)
model.eval()
print(f"[load] {time.perf_counter()-t0:.1f}s, params={sum(p.numel() for p in model.parameters())/1e6:.0f}M")

prompts = [
    "你好，请用一句话介绍你自己。",
    "RISC-V 和 ARM 有什么区别？",
    "把这句话翻译成英文：今天天气很好。",
]
results = []
for pi, user in enumerate(prompts):
    msgs = [{"role": "user", "content": user}]
    enc = tok.apply_chat_template(msgs, add_generation_prompt=True, enable_thinking=False, return_tensors="pt")
    ids = enc.input_ids if hasattr(enc, "input_ids") else enc
    n_in = int(ids.shape[1])
    t1 = time.perf_counter()
    out = model.generate(ids, max_new_tokens=96, do_sample=False,
                         pad_token_id=tok.eos_token_id)
    gen_only = (out[0] if not isinstance(out, dict) else list(out.values())[0][0])[n_in:]
    dt = time.perf_counter() - t1
    n_out = len(gen_only)
    text = tok.decode(gen_only, skip_special_tokens=True)
    results.append({"prompt": user, "input_len": int(n_in), "output": text,
                    "output_tokens": int(n_out), "gen_seconds": round(dt, 3),
                    "tokens_per_sec": round(n_out / dt, 2)})
    print(f"\n=== P{pi}: {user}")
    print(f"[out {n_out} tok / {dt:.2f}s = {n_out/dt:.1f} tok/s]")
    print(text)

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(results, f, ensure_ascii=False, indent=2)
print(f"\ngolden saved -> {OUT}")
