#!/bin/sh
# run_decode_test.sh — full-S4 decode test, WDT-protected, log to persistent fs
cd /mnt/data/kpu_llm
chmod 755 ./llama-cli ./safe_run.sh 2>/dev/null
export LD_LIBRARY_PATH=/mnt/data/kpu_llm
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen
export KPU_RESIDENT=200
export KPU_TILES=s4
export KPU_PRELOAD=1
rm -f /mnt/data/s4_full.log
sh ./safe_run.sh /mnt/data/s4_full.log ./llama-cli -m /mnt/data/models/qwen3-q4km.gguf \
    -c 512 -p '从一数到三十，每个数字用顿号隔开' -n 100 -st --simple-io
