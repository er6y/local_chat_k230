#!/bin/bash
cd /mnt/d/work/git_dev/k230_prj/k230_llm
export KPU_NNCASE_DIR=/tmp/nncase_rt
export KPU_MMZ_SHIM=/mnt/d/work/git_dev/k230_prj/k230_llm/.tools/mmz_shim.c
export PATH=/root/xuantie/bin:$PATH
cmake --build build-riscv-kpu --target llama-bench -j$(nproc) 2>&1 | tail -5
ls -la build-riscv-kpu/bin/llama-bench
