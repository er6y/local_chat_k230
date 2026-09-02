#!/bin/sh
# validate_fix.sh - verify the fs_anchor fix: no abort + performance intact
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export LD_BIND_NOW=1
export MALLOC_CHECK_=3
M=/mnt/data/models/Qwen3-0.6B-Q8_0.gguf
R=/mnt/data/validate_fix.log
rm -f $R
echo "===== validate_fix $(date +%H:%M:%S) =====" >> $R

# 1. pp16 with KPU init (init only, no daemon/preload) - was ABORT before fix
INNER=/mnt/data/vf_pp16.log
rm -f $INNER
echo "----- pp16 KPU-init (was ABORT) -----" >> $R
sh ./safe_run.sh $INNER env KPU_HYBRID=1 KPU_KMODEL_DIR=/mnt/data/kpu_qwen KPU_TILES=s4 ./llama-bench -m $M -t 1 -p 16 -r 1
if grep -q 'corrupted\|Aborted' $INNER; then
  echo "  RESULT: ABORT <<<<" >> $R
else
  echo "  RESULT: CLEAN (fix works)" >> $R
fi
grep -E 'gnne_regs|stats:|corrupted|Aborted|pp16' $INNER | head -5 >> $R

# 2. pp64 + tg32 pure CPU (no KPU env) - verify v2 kernel performance
INNER=/mnt/data/vf_cpu.log
rm -f $INNER
echo "----- pp64+tg32 pure CPU -----" >> $R
sh ./safe_run.sh $INNER ./llama-bench -m $M -t 1 -p 64 -n 32 -r 2
if grep -q 'corrupted\|Aborted' $INNER; then
  echo "  RESULT: ABORT <<<<" >> $R
else
  echo "  RESULT: CLEAN" >> $R
fi
grep -E 'pp64|tg32|corrupted|Aborted' $INNER | head -5 >> $R

# 3. pp16 + tg32 with KPU init (init only, HYBRID=1) - verify no abort + perf
INNER=/mnt/data/vf_kpu.log
rm -f $INNER
echo "----- pp16+tg32 KPU-init HYBRID -----" >> $R
sh ./safe_run.sh $INNER env KPU_HYBRID=1 KPU_KMODEL_DIR=/mnt/data/kpu_qwen KPU_TILES=s4 ./llama-bench -m $M -t 1 -p 16 -n 32 -r 2
if grep -q 'corrupted\|Aborted' $INNER; then
  echo "  RESULT: ABORT <<<<" >> $R
else
  echo "  RESULT: CLEAN" >> $R
fi
grep -E 'pp16|tg32|stats:|corrupted|Aborted' $INNER | head -5 >> $R

sync
echo "===== DONE =====" >> $R
