#!/bin/bash
# build_xt.sh - reconfigure (new project path) + full rebuild llama-bench with Xuantie toolchain
# mirrors the original build-riscv configure flags extracted from the old CMakeCache
set -e
D="$(cd "$(dirname "$0")/../.." && pwd)"
# ensure xuantie toolchain symlink exists inside WSL (single WSL invocation per run)
ln -sfn "$D/llm/build/xuantie" /root/xuantie
ls /root/xuantie/bin/riscv64-unknown-linux-gnu-gcc
cd "$D"
# force reconfigure (toolchain file change requires fresh cache), keep .o files
rm -f build-riscv/CMakeCache.txt
mkdir -p build-riscv
cd build-riscv
cmake ../llamacpp \
  -DCMAKE_TOOLCHAIN_FILE="$D/llm/build/xt-toolchain.cmake" \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DCMAKE_C_FLAGS="-march=rv64gcv -O3" \
  -DCMAKE_CXX_FLAGS="-march=rv64gcv -O3" \
  -DGGML_RVV=ON \
  -DGGML_NATIVE=OFF \
  -DGGML_CPU_ALL_VARIANTS=OFF \
  -DGGML_SSE42=OFF -DGGML_F16C=OFF -DGGML_FMA=OFF \
  -DGGML_BMI2=OFF -DGGML_AVX=OFF -DGGML_AVX2=OFF \
  -DGGML_OPENMP=OFF \
  -DLLAMA_BUILD_TOOLS=ON \
  -DLLAMA_BUILD_EXAMPLES=OFF \
  -DLLAMA_BUILD_SERVER=OFF \
  2>&1 | tail -25
echo "=== CONFIGURE DONE, BUILDING ==="
cmake --build . --target llama-bench -j8 2>&1 | tail -35
echo "=== BUILD DONE ==="
ls -la bin/llama-bench bin/*.so.0.22.0
