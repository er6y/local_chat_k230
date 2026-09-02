#!/bin/sh
# run_perf3.sh - measure prefill wall time using /proc/uptime seconds (float)
MODE="$1"
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export LD_BIND_NOW=1
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen/s4sq_q8
export KPU_RESIDENT=30
export KPU_TILES=s4
export KPU_PRELOAD=0
export GNNE_QUIET=1
if [ "$MODE" = "daemon" ]; then
    export KPU_DAEMON=1
else
    export KPU_LOCAL=1
fi
PROMPT='The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog. A B C D E F G H I J K L M N O P Q R S T U V W X Y Z'
LOG="/mnt/data/perf3_${MODE}.log"
rm -f "$LOG"
t0=$(cut -d' ' -f1 /proc/uptime)
./llama-cli -m /mnt/data/models/Qwen3-0.6B-Q8_0.gguf -c 512 -p "$PROMPT" -n 0 -st --simple-io > "$LOG" 2>&1
t1=$(cut -d' ' -f1 /proc/uptime)
# busybox awk can do float math
awk -v t0="$t0" -v t1="$t1" -v mode="$MODE" 'BEGIN {
  ms = (t1 - t0) * 1000
  printf "=== %s mode ===\n", mode
  printf "wall_time: %.0fms\n", ms
}'
grep -aE 'stats|gemms' "$LOG"
