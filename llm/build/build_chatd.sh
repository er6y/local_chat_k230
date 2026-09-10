#!/bin/bash
# build_chatd.sh - cross-build chatd (supervisor) + kpud (C++ KPU GEMM daemon)
# Products: build-riscv-kpu/bin/{chatd,kpud}
# kpud links the same nncase C++ runtime as libggml-cpu's KPU offload
# (paths per build_kpu_llama.sh: KPU_NNCASE_DIR has include_root/ + nncase_*_runtime_linux/lib)
# NOTE: mmz_shim.c MUST be compiled by the C compiler (C++ mangles the
# __wrap_gnne_* symbols and every wrap falls through)
set -e
export PATH="$HOME/xuantie/bin:$PATH"
CXX=riscv64-unknown-linux-gnu-g++
CC=riscv64-unknown-linux-gnu-gcc

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BIN="$ROOT/build-riscv-kpu/bin"
mkdir -p "$BIN"

KPU_RT="${KPU_NNCASE_DIR:-/tmp/nncase_rt}"
KPU_LIBDIR="$(dirname "$(find "$KPU_RT" -name 'libnncase.rt_modules.k230.a' | head -1)")"
NC_INC="$KPU_RT/include_root/src/Native/include"
GSL_INC="$KPU_RT/include_root/gsl_inc"
MMZ_SHIM="$ROOT/llm/build/mmz_shim.c"

echo "== chatd =="
$CXX -O2 -std=c++17 -Wall \
  "$ROOT/llm/board/chatd.cpp" \
  -o "$BIN/chatd" -lpthread 2>&1 | grep -E "error|Error" | head -20 || true
ls -la "$BIN/chatd"

echo "== kpud =="
$CC -O2 -D_GNU_SOURCE -c "$MMZ_SHIM" -o "$BIN/mmz_shim.o" -march=rv64gc_xtheadcmo
$CXX -O2 -std=c++17 -Wall \
  -I "$NC_INC" -I "$GSL_INC" \
  -c "$ROOT/llm/board/kpud.cpp" -o "$BIN/kpud.o"
$CXX "$BIN/kpud.o" "$BIN/mmz_shim.o" \
  -Wl,--whole-archive "$KPU_LIBDIR/libnncase.rt_modules.k230.a" -Wl,--no-whole-archive \
  "$KPU_LIBDIR/libfunctional_k230.a" \
  "$KPU_LIBDIR/libNncase.Runtime.Native.a" \
  -Wl,--wrap=gnne_enable -Wl,--wrap=gnne_ctrl_set -Wl,--wrap=gnne_disable \
  -Wl,-z,now \
  -o "$BIN/kpud" \
  -lpthread -ldl 2>&1 | grep -E "error|undefined" | sort -u | head -20 || true
ls -la "$BIN/kpud"

echo "== sanity =="
file "$BIN/chatd" "$BIN/kpud"
echo CHATD_BUILD_OK
