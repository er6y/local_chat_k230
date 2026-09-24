#!/bin/sh
# chat_sim.sh - simulate the production chat deployment:
#   default Qwen3 template + --reasoning off (thinking disabled) +
#   standard chat sampling (0.7/0.8/20/rep1.0) + --kpu
#   NOTE: --reasoning off needs the DEFAULT template's reasoning markers;
#   a stripped custom template breaks it. A sacrificial warmup run absorbs
#   the first-run-after-boot startup crash (gnne_regs /dev/mem interplay).
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen/s4sq_q8
M=/mnt/data/models/Qwen3-0.6B-Q8_0.gguf
L=/mnt/data/chat_sim.log
rm -f $L
# sacrificial warmup: first llama-cli after boot may crash once
./llama-cli -m $M -t 1 --single-turn -n 4 -p warmup > /dev/null 2>&1
for P in "你好，我是小王" "你会做什么？" "用一句话介绍一下你自己"; do
  echo "===== TURN: $P =====" >> $L
  sh ./safe_run.sh $L ./llama-cli -m $M -t 1 --kpu --single-turn --reasoning off \
    --temp 0.7 --top-p 0.8 --top-k 20 --repeat-penalty 1.0 -n 64 -p "$P"
done
sync
