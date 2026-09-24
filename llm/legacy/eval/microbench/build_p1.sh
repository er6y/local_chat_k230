#!/bin/bash
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
# build_p1.sh - standalone p1_dump binary (no libggml needed)
set -e
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin/riscv64-buildroot-linux-gnu-gcc
D=$ROOT
"$TC" -O2 -march=rv64gcv_zicbop_zihintpause -static \
  "$D/.tools/p1_dump.c" -o "$D/.tools/p1_dump"
ls -la "$D/.tools/p1_dump"
