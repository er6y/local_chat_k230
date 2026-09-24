#!/bin/bash
# export.sh — HF 权重 → llm-export 导出 onnx（onnx 链第 1 步，其余步骤见 README.md 链条表）
#
# 运行地：本地 WSL（torch/transformers 锁版装在这）
# 用法：./export.sh [模型目录名] [输出目录]
#   ./export.sh                                  # 默认 Qwen2.5-0.5B-Instruct
#   ./export.sh Qwen3-0.6B                       # 换模型（需对应 llm-export 补丁版，见 README）
#   ./export.sh ERNIE-4.5-0.3B /root/pipe_ernie
#
# 权重来源：huggingface.co 官方仓库，下载到 WSL /root/models/<名>/（含 config.json +
# *.safetensors + tokenizer）。若新机器没有：用 HF 镜像
#   HF_ENDPOINT=https://hf-mirror.com huggingface-cli download Qwen/Qwen2.5-0.5B-Instruct \
#     --local-dir /root/models/Qwen2.5-0.5B-Instruct
set -euo pipefail

MODEL=${1:-Qwen2.5-0.5B-Instruct}
DST=${2:-/root/pipe_export}
SRC=/root/models/$MODEL
TOOL=${LLM_EXPORT_TOOL:-/root/llm-export-old}   # 换模型用 ~/llm-export-ernie / ~/llm-export-qwen3

[ -f "$SRC/config.json" ] || { echo "缺权重 $SRC/config.json（见头部下载说明）"; exit 1; }

# 锁版铁律（新版 transformers/torch 会静默产出空图或报错）：
#   torch==2.2.1+cpu  transformers==4.40.2  numpy==1.26.4
#   （qwen3/ernie 导出用各自 fork 时 transformers 可升 4.57.1，见 llm-fork 说明）
python3 - <<'PY'
import torch, transformers, numpy
print("torch", torch.__version__, "| transformers", transformers.__version__, "| numpy", numpy.__version__)
PY

mkdir -p "$DST"
# --test 烧一句中文自证生成连贯；禁用 --onnx_slim（会毁 FakeLinear 重建机制）
python3 "$TOOL"/llmexport/llmexport.py \
  --path "$SRC" \
  --export "$DST/onnx" \
  --dst_path "$DST" \
  --test "今天天气怎么样？"

echo "==== 导出产物 ===="
ls -la "$DST" "$DST/onnx"
echo "预期：onnx/llm.onnx(+.data) + embeddings_bf16.bin + tokenizer.txt + llm_config.json"
echo "下一步：split_lmhead_24.py（lm_head 切块）→ 见本目录 README.md 链条表"
