#!/bin/bash
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
# build_vecdot.sh - compile bench_vecdot with bootlin riscv64 toolchain
set -e
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin/riscv64-buildroot-linux-gnu-gcc
TCXX=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin/riscv64-buildroot-linux-gnu-g++
D=$ROOT
"$TC" -O2 -march=rv64gcv_zicbop_zihintpause -c \
  -I"$D/llamacpp/ggml/include" \
  "$D/.tools/bench_vecdot.c" -o /tmp/bench_vecdot.o
"$TCXX" -O2 -march=rv64gcv_zicbop_zihintpause -static /tmp/bench_vecdot.o \
  "$D/build-riscv-rvv/ggml/src/libggml-cpu.a" \
  "$D/build-riscv-rvv/ggml/src/libggml-base.a" \
  -o "$D/.tools/bench_vecdot" -lpthread -lm
ls -la "$D/.tools/bench_vecdot"
