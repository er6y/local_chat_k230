#!/bin/sh
# selftest_bindnow.sh - selftest with LD_BIND_NOW (PLT lazy-binding was killing
# the very first gnne_ctrl_set call -> KPU never enabled) + MMZ_TRACE to count
# real cache flushes.
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export LD_BIND_NOW=1
export MMZ_TRACE=1
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen_sqtest
export KPU_RESIDENT=10
export KPU_TILES=s4
export KPU_PRELOAD=0
export KPU_SELFTEST=1
rm -f /mnt/data/st4.log
sh ./safe_run.sh /mnt/data/st4.log ./llama-cli -m /mnt/data/models/qwen3-q4km.gguf -c 256 -p '你好' -n 1 -st --simple-io
echo "=== cos ==="
grep -aE 'cos=|preseed' /mnt/data/st4.log | head -9
echo "=== flush count ==="
grep -ac 'FLUSH' /mnt/data/st4.log
grep -a 'FLUSH' /mnt/data/st4.log | head -4
