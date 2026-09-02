#!/bin/sh
# selftest_bn2.sh - LD_BIND_NOW without MMZ_TRACE (the DIAG popen fork seems to
# destabilize the run -> early SIGSEGV; python's no-trace run completed 7 stems)
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export LD_BIND_NOW=1
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen_sqtest
export KPU_RESIDENT=10
export KPU_TILES=s4
export KPU_PRELOAD=0
export KPU_SELFTEST=1
rm -f /mnt/data/st5.log
sh ./safe_run.sh /mnt/data/st5.log ./llama-cli -m /mnt/data/models/qwen3-q4km.gguf -c 256 -p '你好' -n 1 -st --simple-io
echo "=== cos ==="
grep -aE 'cos=|y\[0' /mnt/data/st5.log | head -16
