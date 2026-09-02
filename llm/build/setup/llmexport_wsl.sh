#!/bin/bash
# llmexport_wsl.sh - install deps + convert Qwen3-0.6B to MNN 8bit inside WSL
set -e
export PYPI=https://mirrors.aliyun.com/pypi/simple/
python3 -m pip install -q --break-system-packages torch onnx onnxsim yaspin transformers sentencepiece tiktoken MNN numpy tqdm -i $PYPI 2>&1 | grep -vE 'WARNING|notice' | tail -2
python3 -c "import torch, MNN; print('torch', torch.__version__, '| MNN ok')"
cd /mnt/d/work/git_dev/k230_prj/k230_llm/downloads/MNN/transformers/llm/export
rm -rf /mnt/d/work/git_dev/k230_prj/k230_llm/models/Qwen3-0.6B-MNN-8bit
python3 llmexport.py \
  --path /mnt/d/work/git_dev/k230_prj/k230_llm/models/Qwen3-0.6B-hf \
  --export mnn --quant_bit 8 \
  --dst_path /mnt/d/work/git_dev/k230_prj/k230_llm/models/Qwen3-0.6B-MNN-8bit \
  2>&1 | tail -6
ls -la /mnt/d/work/git_dev/k230_prj/k230_llm/models/Qwen3-0.6B-MNN-8bit/
echo "LLMEXPORT_WSL_DONE"
