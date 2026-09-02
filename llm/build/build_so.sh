#!/bin/bash
# build_so.sh - rebuild libggml-cpu with KPU_SELFTEST support
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT/build-riscv-kpu"
cmake --build . --target ggml-cpu -j 8 2>&1 | tail -5
ls -la bin/libggml-cpu.so.0.22.0
md5sum bin/libggml-cpu.so.0.22.0
