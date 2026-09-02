#!/bin/bash
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
# build_smoke.sh - build static RVV smoke test + riscv llama-quantize
set -e
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin
SRC=$ROOT/llamacpp
BUILD=$ROOT/build-riscv-rvv

echo "=== build rvv_smoke (static) ==="
$TC/riscv64-buildroot-linux-gnu-gcc -march=rv64gcv_zicbop_zihintpause -mabi=lp64d -O2 -static \
  $ROOT/llm/build/rvv_smoke.c \
  -o $ROOT/llm/build/rvv_smoke
ls -la $ROOT/llm/build/rvv_smoke
echo "SMOKE_BUILD_OK"

echo "=== build llama-quantize (static) ==="
cd $BUILD
make -j$(nproc) llama-quantize 2>&1 | tail -2
ls -la $BUILD/bin/llama-quantize
$TC/riscv64-buildroot-linux-gnu-readelf -d $BUILD/bin/llama-quantize | grep -c NEEDED || true
echo "QUANTIZE_BUILD_OK"
