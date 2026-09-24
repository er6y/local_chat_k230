#!/bin/bash
# build_llmd.sh - cross-build the resident LLM daemon against the KPU llama libs
# Output: build-riscv-kpu/bin/llmd
set -e
export PATH="$HOME/xuantie/bin:$PATH"
CXX=riscv64-unknown-linux-gnu-g++

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BIN="$ROOT/build-riscv-kpu/bin"
SRC="$ROOT/llm/llamacpp"

# libllama.so / libllama-common.so live in their build subdirs
LLAMA_LIB="$(dirname "$(find "$ROOT/build-riscv-kpu" -name 'libllama.so*' | head -1)")"
COMMON_LIB="$(dirname "$(find "$ROOT/build-riscv-kpu" -name 'libllama-common.so*' | head -1)")"
echo "libllama dir: $LLAMA_LIB"
echo "libcommon dir: $COMMON_LIB"

$CXX -O2 -std=c++17 -fPIC \
  -I "$SRC/include" -I "$SRC/common" -I "$SRC/ggml/include" \
  "$ROOT/llm/llmd/llmd.cpp" \
  -L "$BIN" -L "$LLAMA_LIB" -L "$COMMON_LIB" \
  -l:libllama.so.0.3.0 -l:libllama-common.so.0.3.0 \
  -l:libggml.so.0.22.0 -l:libggml-cpu.so.0.22.0 -l:libggml-base.so.0.22.0 \
  -o "$BIN/llmd" \
  -Wl,-rpath,'$ORIGIN' 2>&1 | grep -E "error|Error" | head -30 || true

ls -la "$BIN/llmd"
"$HOME/xuantie/bin/riscv64-unknown-linux-gnu-readelf" -d "$BIN/llmd" | grep NEEDED
echo LLMD_BUILD_OK
