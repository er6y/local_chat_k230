#!/bin/bash
# build_microbench.sh - compile C908 microbench + xtheadvector probe with bootlin GCC in WSL
set -e
CC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin/riscv64-buildroot-linux-gnu-gcc
T=/mnt/d/work/git_dev/k230_prj/k230_llm/.tools

$CC -O2 -march=rv64gcv_zicbop_zihintpause -mabi=lp64d -static \
    -o $T/c908_microbench $T/c908_microbench.c

$CC -O2 -march=rv64gc_xtheadvector -mabi=lp64d -static \
    -o $T/thvector_probe $T/thvector_probe.c

ls -la $T/c908_microbench $T/thvector_probe
echo "MICROBENCH_BUILD_DONE"
