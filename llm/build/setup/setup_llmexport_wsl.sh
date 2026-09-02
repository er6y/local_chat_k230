#!/bin/bash
# setup_llmexport_wsl.sh - install llmexport deps inside WSL (Linux native)
set -e
python3 --version
pip3 --version 2>/dev/null || { apt-get update -qq && apt-get install -y -qq python3-pip; }
pip3 install -q --break-system-packages torch onnx onnxsim yaspin transformers sentencepiece tiktoken MNN numpy tqdm -i https://pypi.tuna.tsinghua.edu.cn/simple 2>&1 | tail -3
python3 -c "import torch, MNN; print('torch', torch.__version__, '| MNN ok')"
which mnnconvert || python3 -c "from MNN.tools import mnnconvert; print('pymnn mnnconvert entry ok')"
echo "WSL_SETUP_DONE"
