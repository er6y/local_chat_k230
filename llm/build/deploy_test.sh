#!/bin/bash
# deploy_test.sh - build, push, and run the selftest in one shot (WSL side)
set -e
bash /mnt/d/work/git_dev/k230_prj/k230_llm/llm/build/build_so.sh
cp /mnt/d/work/git_dev/k230_prj/k230_llm/build-riscv-kpu/bin/libggml-cpu.so.0.22.0 \
   /mnt/d/work/git_dev/k230_prj/k230_llm/llm/build/push_lib/
echo "BUILD OK"
