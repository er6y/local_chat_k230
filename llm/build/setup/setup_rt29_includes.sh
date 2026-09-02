#!/bin/bash
# setup_rt29_includes.sh - assemble include_root/ for the nncase 2.9 runtime
# (the 2.11 one was hand-assembled by the predecessor; 2.9 tgz ships the full
# Native include tree itself, only gsl_inc needs to be reused from 2.11)
set -e
RT29=/tmp/nncase_rt29
mkdir -p $RT29/include_root/src/Native/include
cp -r $RT29/nncase_k230_v2.9.0_runtime_linux/include/. $RT29/include_root/src/Native/include/
cp -r /tmp/nncase_rt/include_root/gsl_inc $RT29/include_root/
echo "=== include_root ready ==="
ls $RT29/include_root/src/Native/include/nncase/runtime/ | head -5
ls $RT29/include_root/gsl_inc/
# quick sanity: the four headers kpu_gemm.cpp includes
for h in api.h runtime/simple_types.h runtime/interpreter.h runtime/runtime_tensor.h; do
  test -f $RT29/include_root/src/Native/include/nncase/$h && echo "OK nncase/$h" || echo "MISSING nncase/$h"
done
