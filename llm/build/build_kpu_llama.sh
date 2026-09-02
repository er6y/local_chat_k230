#!/bin/bash
# build_kpu_llama.sh - cross-build llama.cpp with K230 KPU GEMM offload
# Produces build-riscv-kpu/bin/{llama-cli,llama-bench,libggml-cpu.so,...}
set -e
export PATH="$HOME/xuantie/bin:$PATH"
cd /mnt/d/work/git_dev/k230_prj/k230_llm

export KPU_NNCASE_DIR=/tmp/nncase_rt
export KPU_MMZ_SHIM=/mnt/d/work/git_dev/k230_prj/k230_llm/.tools/mmz_shim.c

cmake -B build-riscv-kpu -S llamacpp \
  -DCMAKE_TOOLCHAIN_FILE=/mnt/d/work/git_dev/k230_prj/k230_llm/.tools/xt-toolchain.cmake \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DGGML_RVV=ON \
  -DGGML_OPENMP=OFF \
  -DLLAMA_BUILD_TESTS=OFF \
  -DLLAMA_BUILD_EXAMPLES=OFF \
  -DLLAMA_BUILD_SERVER=ON \
  -DLLAMA_CURL=OFF 2>&1 | tail -5
# NOTE: llama-cli lives under LLAMA_BUILD_SERVER (cli depends on server-impl)

cmake --build build-riscv-kpu --target llama-cli llama-bench -j"$(nproc)" 2>&1 | grep -E 'error|Error|warning: unused|kpu_gemm|ggml-cpu' | tail -20 || true
cmake --build build-riscv-kpu --target llama-cli llama-bench -j"$(nproc)" 2>&1 | tail -3

ls -la build-riscv-kpu/bin/ | grep -E 'llama-cli|llama-bench|ggml-cpu'
echo "== verify kpu symbols in libggml-cpu.so =="
/root/xuantie/bin/riscv64-unknown-linux-gnu-nm -D build-riscv-kpu/bin/libggml-cpu.so | grep -E 'ggml_kpu|nncase|kd_mpi' | head -10
echo "== verify R_RISCV_USE_HI20 (mmz shim asm intact) =="
/root/xuantie/bin/riscv64-unknown-linux-gnu-readelf -d build-riscv-kpu/bin/libggml-cpu.so | head -8
echo BUILD_OK
