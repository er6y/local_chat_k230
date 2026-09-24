#!/bin/bash
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
# build_q8bench.sh - build bench_q8gemv static with xuantie toolchain (WSL side)
set -e
TC=/root/xuantie/bin/riscv64-unknown-linux-gnu-gcc
D=$ROOT
"$TC" -O3 -march=rv64gcv_zfh_zvfh_zicbop_zihintpause -mabi=lp64d -static \
  "$D/.tools/bench_q8gemv.c" -o "$D/.tools/bench_q8gemv" -lm
ls -la "$D/.tools/bench_q8gemv"
echo BUILD_OK
