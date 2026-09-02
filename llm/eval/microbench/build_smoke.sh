#!/bin/bash
# build_smoke.sh - build static RVV smoke test + riscv llama-quantize
set -e
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin
SRC=/mnt/d/work/git_dev/k230_prj/k230_llm/llamacpp
BUILD=/mnt/d/work/git_dev/k230_prj/k230_llm/build-riscv-rvv

echo "=== build rvv_smoke (static) ==="
$TC/riscv64-buildroot-linux-gnu-gcc -march=rv64gcv_zicbop_zihintpause -mabi=lp64d -O2 -static \
  /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/rvv_smoke.c \
  -o /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/rvv_smoke
ls -la /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/rvv_smoke
echo "SMOKE_BUILD_OK"

echo "=== build llama-quantize (static) ==="
cd $BUILD
make -j$(nproc) llama-quantize 2>&1 | tail -2
ls -la $BUILD/bin/llama-quantize
$TC/riscv64-buildroot-linux-gnu-readelf -d $BUILD/bin/llama-quantize | grep -c NEEDED || true
echo "QUANTIZE_BUILD_OK"
