#!/bin/bash
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
# build_vendor_test.sh - Cross-compile t_vendor.cpp against vendor nncase runtime
set -e

NNCASE_RT=/tmp/nncase_rt/nncase_k230_v2.11.0_runtime_linux
NNCASE_INC=/tmp/nncase_rt/nncase_k230_v2.11.0_runtime_linux/include
GSL_INC=/tmp/nncase_rt/include_root/gsl_inc
SRC_INC=/tmp/nncase_rt/include_root/src/Native/include

# Use the Xuantie cross-compiler from the existing build environment
CXX=/root/xuantie/bin/riscv64-unknown-linux-gnu-g++
if ! [ -x "$CXX" ]; then
    echo "ERROR: cannot find riscv64 cross-compiler at $CXX"
    exit 1
fi

echo "Using CXX: $CXX"
$CXX --version | head -1

SRC=$ROOT/llm/build/t_vendor.cpp
SHIM=$ROOT/llm/build/mmz_shim.c
OUT=$ROOT/llm/build/t_vendor

# Include paths:
# - NNCASE_INC: the runtime package headers
# - SRC_INC: fallback for headers not in the runtime package
# - GSL_INC: gsl-lite
INCLUDES="-I$NNCASE_INC -I$SRC_INC -I$GSL_INC"

# Libraries: link order matters
# rt_modules.k230 contains the K230 module registration
# functional_k230 contains K230 kernel implementations
# Nncase.Runtime.Native contains the core runtime (interpreter, tensor, etc.)
LIBS="-Wl,--whole-archive $NNCASE_RT/lib/libnncase.rt_modules.k230.a -Wl,--no-whole-archive \
      $NNCASE_RT/lib/libfunctional_k230.a \
      $NNCASE_RT/lib/libNncase.Runtime.Native.a"

# Flags matching the existing k230_llm build
CXXFLAGS="-march=rv64gcv_zfh_zvfh_zicbop_zihintpause -mabi=lp64d -O2 -std=c++17 \
       -DNNCASE_RUNTIME_USE_K230 -D_GLIBCXX_USE_CXX11_ABI=0 \
       -Wl,--allow-multiple-definition -Wl,--unresolved-symbols=ignore-in-shared-libs"
# mmz_shim.c needs xtheadcmo extension for th.dcache.civa instruction
CFLAGS="-march=rv64gc_xtheadcmo -mabi=lp64d -O2 -DNNCASE_RUNTIME_USE_K230 -D_GNU_SOURCE"

echo "Compiling mmz_shim_min.c (C, xtheadcmo)..."
CC=/root/xuantie/bin/riscv64-unknown-linux-gnu-gcc
# Use minimal mmz shim (no gnne wrap) - let vendor runtime use native gnne_enable
$CC $CFLAGS -c /tmp/mmz_shim_min.c -o /tmp/mmz_shim.o 2>&1

echo "Compiling t_vendor.cpp (C++)..."
$CXX $CXXFLAGS $INCLUDES -o $OUT $SRC /tmp/mmz_shim.o $LIBS \
    -lpthread -ldl -lm -lstdc++ 2>&1

echo "=== Build result ==="
ls -lh $OUT 2>/dev/null && echo "SUCCESS" || echo "FAILED"
