#!/bin/bash
# kick_build.sh - clean stale merged kmodels, start full 196-kmodel build in background
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT/llm/kmodels/compile"
rm -f /tmp/kpu_poc/qwen/l*_*.kmodel /tmp/kpu_poc/qwen/*.onnx \
      /tmp/kpu_poc/qwen/l0_qkv* /tmp/kpu_poc/qwen/l0_gu* /tmp/kpu_poc/qwen/l0_down* \
      /tmp/kpu_poc/qwen/l0_o* /tmp/kpu_poc/qwen/l0_q.* /tmp/kpu_poc/qwen/l0_k.* \
      /tmp/kpu_poc/qwen/l0_v.* /tmp/kpu_poc/qwen/l0_gate* /tmp/kpu_poc/qwen/l0_up*
nohup python3 make_qwen_kmodels.py all --jobs 4 > /tmp/kpu_poc/qwen/build_all.log 2>&1 &
echo "BG_PID=$!"
sleep 5
echo "--- log head ---"
head -5 /tmp/kpu_poc/qwen/build_all.log
pgrep -fa make_qwen | head -6
