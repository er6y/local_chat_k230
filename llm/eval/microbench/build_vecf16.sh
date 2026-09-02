#!/bin/bash
# build_vecf16.sh - compile bench_vecf16 with bootlin riscv64 toolchain
set -e
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin/riscv64-buildroot-linux-gnu-gcc
TCXX=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin/riscv64-buildroot-linux-gnu-g++
D=/mnt/d/work/git_dev/k230_prj/k230_llm
"$TC" -O2 -march=rv64gcv_zicbop_zihintpause -c \
  -I"$D/llamacpp/ggml/include" \
  -I"$D/llamacpp/ggml/src" -I"$D/llamacpp/ggml/src/ggml-cpu" \
  "$D/.tools/bench_vecf16.c" -o /tmp/bench_vecf16.o
"$TCXX" -O2 -march=rv64gcv_zicbop_zihintpause -static /tmp/bench_vecf16.o \
  "$D/build-riscv-rvv/ggml/src/libggml-cpu.a" \
  "$D/build-riscv-rvv/ggml/src/libggml-base.a" \
  -o "$D/.tools/bench_vecf16" -lpthread -lm
ls -la "$D/.tools/bench_vecf16"
