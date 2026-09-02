#!/bin/bash
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd $ROOT
export KPU_NNCASE_DIR=/tmp/nncase_rt
export KPU_MMZ_SHIM=$ROOT/llm/build/mmz_shim.c
export PATH=/root/xuantie/bin:$PATH
cmake --build build-riscv-kpu --target llama-bench -j$(nproc) 2>&1 | tail -5
ls -la build-riscv-kpu/bin/llama-bench
