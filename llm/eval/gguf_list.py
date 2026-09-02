#!/usr/bin/env python3
# gguf_list.py - list qwen3 gguf tensors + check dequant capability
import sys
sys.path.insert(0, "/mnt/d/work/git_dev/k230_prj/k230_llm/llamacpp/gguf-py")
import gguf
print("gguf pkg:", gguf.__file__)
print("has dequantize:", hasattr(gguf, "dequantize"))
from gguf import GGUFReader
r = GGUFReader("/root/qwen3-q4km.gguf")
print("=== arch meta ===")
for k in ["general.architecture", "qwen3.block_count", "qwen3.context_length",
          "qwen3.embedding_length", "qwen3.attention.head_count",
          "qwen3.attention.head_count_kv", "qwen3.feed_forward_length"]:
    if k in r.fields:
        f = r.fields[k]
        print(k, "=", f.parts[f.data[0]][0] if f.types[0] == gguf.GGUFValueType.INT32 or True else None)
print("=== layer-0 tensors ===")
n = 0
for t in r.tensors:
    if ".0." in t.name or "token_embd" in t.name or "output" in t.name:
        print(f"{t.name:45s} {str(t.tensor_type):30s} shape={tuple(t.shape)} bytes={t.n_bytes}")
        n += 1
    if n > 25: break
print("total tensors:", len(r.tensors))
