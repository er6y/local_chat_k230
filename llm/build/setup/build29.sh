#!/bin/bash
# build29.sh - reconfigure + rebuild libggml-cpu linked against nncase 2.9 runtime
set -e
cd /mnt/d/work/git_dev/local_chat_k230/build-riscv-kpu
KPU_NNCASE_DIR=/tmp/nncase_rt29 \
KPU_MMZ_SHIM=/mnt/d/work/git_dev/local_chat_k230/.tools/mmz_shim.c \
cmake . 2>&1 | grep -E "K230 KPU offload|Error|error" || true
touch /mnt/d/work/git_dev/local_chat_k230/llamacpp/ggml/src/ggml-cpu/kpu_gemm.cpp
cmake --build . --target ggml-cpu -j 8 2>&1 | tail -25
ls -la bin/libggml-cpu.so.0.22.0
md5sum bin/libggml-cpu.so.0.22.0
echo BUILD29_DONE
