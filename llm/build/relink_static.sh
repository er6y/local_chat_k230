#!/bin/bash
# relink_static.sh - find offending symbols, then relink binaries fully static (glibc 2.33-proof)
set -e
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin
BUILD=/mnt/d/work/git_dev/local_chat_k230/build-riscv-rvv
SRC=/mnt/d/work/git_dev/local_chat_k230/llamacpp

echo "=== which symbols need > 2.33 ==="
$TC/riscv64-buildroot-linux-gnu-objdump -T $BUILD/bin/llama-bench 2>/dev/null | grep -E 'GLIBC_2\.(3[4-9]|[4-9][0-9])' | awk '{print $NF, $(NF-1)}' | sort -u | head -15

echo "=== reconfigure with -static and relink ==="
cd $BUILD
cmake $SRC \
  -DCMAKE_C_COMPILER=$TC/riscv64-buildroot-linux-gnu-gcc \
  -DCMAKE_CXX_COMPILER=$TC/riscv64-buildroot-linux-gnu-g++ \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=OFF \
  -DGGML_RVV=ON \
  -DGGML_NATIVE=OFF \
  -DGGML_OPENMP=OFF \
  -DGGML_BACKEND_DL=OFF \
  -DCMAKE_SYSTEM_PROCESSOR=riscv64 \
  -DCMAKE_EXE_LINKER_FLAGS="-static" \
  -DCMAKE_C_FLAGS="-march=rv64gcv_zicbop_zihintpause -mabi=lp64d" \
  -DCMAKE_CXX_FLAGS="-march=rv64gcv_zicbop_zihintpause -mabi=lp64d" \
  > /dev/null 2>&1 || cmake . -DCMAKE_EXE_LINKER_FLAGS="-static" > /dev/null 2>&1

make -j$(nproc) llama-bench llama-completion 2>&1 | tail -3

echo "=== verify static ==="
for f in llama-bench llama-completion; do
    echo "--- $f ---"
    ls -la $BUILD/bin/$f
    $TC/riscv64-buildroot-linux-gnu-readelf -d $BUILD/bin/$f 2>/dev/null | grep -c NEEDED || true
    echo "(0 NEEDED = fully static)"
done
echo "RELINK DONE"
