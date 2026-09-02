#!/bin/sh
# gnne_trace.sh - run selftest while sampling GNNE registers at high rate
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen_sqtest
export KPU_RESIDENT=10
export KPU_TILES=s4
export KPU_PRELOAD=0
export KPU_SELFTEST=1
rm -f /tmp/gt.log
./llama-cli -m /mnt/data/models/qwen3-q4km.gguf -c 256 -p '你好' -n 1 -st --simple-io > /tmp/gt.log 2>&1 &
LP=$!
# sample the status window while the selftest runs
i=0
while [ $i -lt 300 ]; do
    kill -0 $LP 2>/dev/null || break
    if grep -aq selftest /tmp/gt.log 2>/dev/null; then
        echo "T=$(date +%s%3N) PC=$(devmem 0x80400100) ST40=$(devmem 0x80400130) ST50=$(devmem 0x80400050) C128=$(devmem 0x80400128)"
    fi
    i=$((i+1))
done &
SAMP=$!
wait $LP 2>/dev/null
kill $SAMP 2>/dev/null
echo "=== selftest result ==="
grep -aE 'cos=|preseed' /tmp/gt.log | head -9
