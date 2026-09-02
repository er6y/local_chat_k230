#!/bin/bash
# deploy_test.sh - build, push, and run the selftest in one shot (WSL side)
set -e
bash /mnt/d/work/git_dev/local_chat_k230/llm/build/build_so.sh
cp /mnt/d/work/git_dev/local_chat_k230/build-riscv-kpu/bin/libggml-cpu.so.0.22.0 \
   /mnt/d/work/git_dev/local_chat_k230/llm/build/push_lib/
echo "BUILD OK"
