#!/bin/bash
# rebuild_quick.sh - incremental rebuild of KPU llama targets after kpu_gemm edits
set -e
export KPU_NNCASE_DIR=/tmp/nncase_rt
export KPU_MMZ_SHIM=/mnt/d/work/git_dev/local_chat_k230/llm/build/mmz_shim.c
cd /mnt/d/work/git_dev/local_chat_k230
cmake --build build-riscv-kpu --target llama-cli llama-bench -j"$(nproc)" 2>&1 | tail -4
ls -la build-riscv-kpu/bin/libggml-cpu.so.0.22.0
echo REBUILD_OK
