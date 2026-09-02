#!/bin/bash
# build_fastk_verify.sh - cross-build the hardcoded-decode verifier
set -e
BIN=/mnt/d/work/git_dev/k230_prj/k230_llm/build-riscv-kpu/bin
CXX=/root/xuantie/bin/riscv64-unknown-linux-gnu-g++
CC=/root/xuantie/bin/riscv64-unknown-linux-gnu-gcc
LLM=/mnt/d/work/git_dev/k230_prj/k230_llm/llamacpp
INC="-I$LLM/include -I$LLM/ggml/include -I$LLM/ggml/src -I$LLM/src"
FLAGS="-march=rv64gcv_zfh_zvfh_zicbop_zihintpause -mabi=lp64d -O2 -std=c++17"
OUT=/mnt/d/work/git_dev/k230_prj/k230_llm/.tools/fastk_verify

echo "compiling fastk_verify..."
$CXX $FLAGS -O2 -c /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/fastk_verify.c -o /tmp/fastk_verify.o $INC
$CXX $FLAGS $INC -o $OUT /tmp/fastk_verify.o \
  /mnt/d/work/git_dev/k230_prj/k230_llm/llamacpp/src/llama-fast-k230.cpp \
  -L$BIN -l:libllama.so.0.3.0 -lggml-cpu -lggml-base -lggml \
  -Wl,-rpath-link,$BIN -lpthread -ldl -lm -lstdc++
echo "=== result ==="
ls -lh $OUT
/root/xuantie/bin/riscv64-unknown-linux-gnu-nm -D $BIN/libllama.so | grep -c "llama_model_load_from_file" || true
