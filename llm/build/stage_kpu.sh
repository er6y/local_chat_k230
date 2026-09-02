#!/bin/bash
# stage_kpu.sh - stage all board artifacts to /mnt/d (windows-visible) for adb push
# run AFTER the 196-kmodel build completes (checks count).
set -e
STAGE=/mnt/d/work/git_dev/local_chat_k230/llm/build/stage_kpu
rm -rf $STAGE
mkdir -p $STAGE/kpu_qwen $STAGE/kpu_llm

N=$(ls /tmp/kpu_poc/qwen/l*.kmodel 2>/dev/null | wc -l)
echo "kmodels built: $N"
if [ "$N" -lt 196 ]; then
    echo "WARN: expected 196 kmodels, got $N (build still running?)"
fi
cp /tmp/kpu_poc/qwen/l*.kmodel $STAGE/kpu_qwen/

# llama binaries + libs
BIN=/mnt/d/work/git_dev/local_chat_k230/build-riscv-kpu/bin
cp $BIN/llama-cli $BIN/llama-bench $STAGE/kpu_llm/
cp $BIN/lib*.so* $STAGE/kpu_llm/

# kpu_run + layer0 probe input (numeric verification)
cp /mnt/d/work/git_dev/local_chat_k230/llm/eval/m1/kpu_run $STAGE/kpu_llm/ 2>/dev/null || true
python3 - <<'EOF'
import numpy as np
x = np.load("/tmp/kpu_poc/qwen/l0_gate.probe_x.npy")
x.tofile("/mnt/d/work/git_dev/local_chat_k230/llm/build/stage_kpu/l0_gate.probe_x.bin")
print("probe bin:", x.shape, x.dtype)
EOF

du -sh $STAGE/*
ls $STAGE/kpu_llm | wc -l
echo STAGE_OK
