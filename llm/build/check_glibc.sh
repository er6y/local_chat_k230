#!/bin/bash
# check_glibc.sh - verify new RVV binaries don't need glibc newer than 2.33 (board's version)
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1/bin
BUILD=/mnt/d/work/git_dev/local_chat_k230/build-riscv-rvv

echo "=== binaries built ==="
ls -la $BUILD/bin/ | grep -E 'llama-bench|llama-completion'

echo "=== glibc symbol versions referenced (must be <= GLIBC_2.33) ==="
for f in $BUILD/bin/llama-bench $BUILD/bin/llama-completion; do
    echo "--- $f ---"
    $TC/riscv64-buildroot-linux-gnu-objdump -T $f 2>/dev/null | grep -oE 'GLIBC_[0-9.]+' | sort -uV | tail -5
    echo "(dynamic libs needed:)"
    $TC/riscv64-buildroot-linux-gnu-readelf -d $f 2>/dev/null | grep NEEDED
done
echo "=== check done ==="
