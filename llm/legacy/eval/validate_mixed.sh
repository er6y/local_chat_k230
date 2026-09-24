#!/bin/sh
# validate_mixed.sh - KPU mixed mode (daemon+preload) + Chinese quality
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export LD_BIND_NOW=1
M=/mnt/data/models/Qwen3-0.6B-Q8_0.gguf
R=/mnt/data/validate_mixed.log
rm -f $R
echo "===== validate_mixed $(date +%H:%M:%S) =====" >> $R

# 1. pp128 mixed KPU+CPU (daemon + preload + HYBRID) - verify no abort + perf
INNER=/mnt/data/vm_pp128.log
rm -f $INNER
echo "----- pp128 mixed KPU+CPU -----" >> $R
sh ./safe_run.sh $INNER env \
  KPU_HYBRID=1 KPU_DAEMON=1 KPU_KMODEL_DIR=/mnt/data/kpu_qwen \
  KPU_TILES=s4 KPU_PRELOAD=1 KPU_RESIDENT=200 \
  ./llama-bench -m $M -t 1 -p 128 -r 2
if grep -q 'corrupted\|Aborted' $INNER; then
  echo "  RESULT: ABORT <<<<" >> $R
else
  echo "  RESULT: CLEAN" >> $R
fi
grep -E 'pp128|stats:|corrupted|Aborted' $INNER | head -5 >> $R

# 2. Chinese quality test (mixed mode)
INNER=/mnt/data/vm_chinese.log
rm -f $INNER
echo "----- Chinese quality (mixed) -----" >> $R
sh ./safe_run.sh $INNER env \
  KPU_HYBRID=1 KPU_DAEMON=1 KPU_KMODEL_DIR=/mnt/data/kpu_qwen \
  KPU_TILES=s4 KPU_PRELOAD=1 KPU_RESIDENT=200 \
  ./llama-cli -m $M -t 1 -p '浣犲ソ锛岃鐢ㄤ腑鏂囦粙缁嶄竴涓嬭嚜宸? -n 80 --no-display-prompt
if grep -q 'corrupted\|Aborted' $INNER; then
  echo "  RESULT: ABORT <<<<" >> $R
else
  echo "  RESULT: CLEAN" >> $R
fi
grep -E 'stats:|corrupted|Aborted' $INNER | head -3 >> $R
# show the generated text (last 15 lines before stats)
grep -v '^\[' $INNER | tail -20 >> $R

sync
echo "===== DONE =====" >> $R
