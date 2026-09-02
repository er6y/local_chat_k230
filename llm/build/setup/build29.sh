#!/bin/bash
# build29.sh - reconfigure + rebuild libggml-cpu linked against nncase 2.9 runtime
set -e
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd "$ROOT/build-riscv-kpu"
KPU_NNCASE_DIR=/tmp/nncase_rt29 \
KPU_MMZ_SHIM="$ROOT/llm/build/mmz_shim.c" \
cmake . 2>&1 | grep -E "K230 KPU offload|Error|error" || true
touch "$ROOT/llm/llamacpp/ggml/src/ggml-cpu/kpu_gemm.cpp"
cmake --build . --target ggml-cpu -j 8 2>&1 | tail -25
ls -la bin/libggml-cpu.so.0.22.0
md5sum bin/libggml-cpu.so.0.22.0
echo BUILD29_DONE
