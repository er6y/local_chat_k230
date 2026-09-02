#!/bin/bash
# deploy_test.sh - build, push, and run the selftest in one shot (WSL side)
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
bash "$ROOT/llm/build/build_so.sh"
cp "$ROOT/build-riscv-kpu/bin/libggml-cpu.so.0.22.0" \
   "$ROOT/llm/build/push_lib/"
echo "BUILD OK"
