#!/bin/sh
# selftest_bn29.sh - 2.9-runtime selftest: same as bn2 but KMODEL_DIR -> sqtest29
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export LD_BIND_NOW=1
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen_sqtest29
export KPU_RESIDENT=10
export KPU_TILES=s4
export KPU_PRELOAD=0
export KPU_SELFTEST=1
rm -f /mnt/data/st29.log
sh ./safe_run.sh /mnt/data/st29.log ./llama-cli -m /mnt/data/models/qwen3-q4km.gguf -c 256 -p '浣犲ソ' -n 1 -st --simple-io
echo "=== cos ==="
grep -aE 'cos=|y\[0' /mnt/data/st29.log | head -16
