#!/bin/bash
# inc_build_cpu.sh — 增量重编 libggml-cpu.so（只动 kpu_gemm.cpp 时用，避免全量重编）
set -e
export PATH="$HOME/xuantie/bin:$PATH"
ROOT=/mnt/d/work/git_dev/k230_prj/local_chat_k230
cd "$ROOT"
export KPU_NNCASE_DIR=/tmp/nncase_rt
export KPU_MMZ_SHIM="$ROOT/llm/build/mmz_shim.c"
cmake --build build-riscv-kpu --target ggml-cpu -j"$(nproc)" 2>&1 | tail -6
ls -la build-riscv-kpu/bin/libggml-cpu.so.0.22.0
"$HOME/xuantie/bin/riscv64-unknown-linux-gnu-nm" -D build-riscv-kpu/bin/libggml-cpu.so \
  | grep -cE 'ggml_kpu|nncase' || true
md5sum build-riscv-kpu/bin/libggml-cpu.so.0.22.0
echo INC_BUILD_OK
