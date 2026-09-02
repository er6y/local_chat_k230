#!/bin/bash
# build_rvv.sh - rebuild llama.cpp for K230 C908 with RVV intrinsics (static, glibc-2.33-proof)
set -e
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin
SRC=/mnt/d/work/git_dev/local_chat_k230/llamacpp
BUILD=/mnt/d/work/git_dev/local_chat_k230/build-riscv-rvv

rm -rf $BUILD
mkdir -p $BUILD && cd $BUILD
cmake $SRC \
  -DCMAKE_SYSTEM_NAME=Linux \
  -DCMAKE_SYSTEM_PROCESSOR=riscv64 \
  -DCMAKE_C_COMPILER=$TC/riscv64-buildroot-linux-gnu-gcc \
  -DCMAKE_CXX_COMPILER=$TC/riscv64-buildroot-linux-gnu-g++ \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=OFF \
  -DGGML_RVV=ON \
  -DGGML_RV_ZFH=OFF \
  -DGGML_RV_ZVFH=OFF \
  -DGGML_RV_ZVFBFWMA=OFF \
  -DGGML_NATIVE=OFF \
  -DGGML_OPENMP=OFF \
  -DGGML_BACKEND_DL=OFF \
  -DLLAMA_BUILD_TESTS=OFF \
  -DLLAMA_BUILD_EXAMPLES=OFF \
  -DLLAMA_BUILD_SERVER=OFF \
  2>&1 | grep -iE 'riscv|rvv|march|error' | head -10

make -j$(nproc) llama-bench llama-completion 2>&1 | tail -5

echo "=== verify vector instructions in libggml-cpu.a ==="
OBJDUMP=$TC/riscv64-buildroot-linux-gnu-objdump
AR=$TC/riscv64-buildroot-linux-gnu-ar
$AR x $BUILD/ggml/src/libggml-cpu.a ggml-cpu/arch/riscv/quants.c.o 2>/dev/null || $AR x $BUILD/ggml/src/libggml-cpu.a quants.c.o 2>/dev/null || true
QOBJ=$(find $BUILD -name 'quants.c.o' | head -1)
echo "quants.o: $QOBJ"
$OBJDUMP -d $QOBJ | grep -cE 'vsetvl|vle[0-9]+\.v|vmacc' || echo "0 VECTOR INSNS - FATAL"
echo "=== binaries ==="
ls -la $BUILD/bin/llama-bench $BUILD/bin/llama-completion 2>/dev/null || find $BUILD -name 'llama-bench' -o -name 'llama-completion' | head -4
file $BUILD/bin/llama-bench 2>/dev/null | head -1
echo "BUILD_RVV_DONE"
