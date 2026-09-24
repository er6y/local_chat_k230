#!/bin/sh
# samp_ab.sh - quantify sampling overhead: bench(no sampling) vs cli(greedy) vs cli(chat sampling)
# plus a --kpu flag end-to-end check (pp128 should land ~10.7 t/s, gemms 579)
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
M=/mnt/data/models/Qwen3-0.6B-Q8_0.gguf
L=/mnt/data/samp_ab.log
rm -f $L
P="请介绍一下中国的四大发明"
echo "===== A: llama-bench tg32 (pure decode, NO sampling) =====" >> $L
sh ./safe_run.sh $L ./llama-bench -m $M -t 1 -n 32 -r 2
echo "===== B: llama-cli greedy (--temp 0 --top-k 1) =====" >> $L
sh ./safe_run.sh $L ./llama-cli -m $M -t 1 --single-turn --temp 0 --top-k 1 -n 64 -p "$P"
echo "===== C: llama-cli chat sampling (--temp 0.7 --top-p 0.8 --top-k 20) =====" >> $L
sh ./safe_run.sh $L ./llama-cli -m $M -t 1 --single-turn --temp 0.7 --top-p 0.8 --top-k 20 -n 64 -p "$P"
echo "===== D: --kpu flag end-to-end (pp128, expect ~10.7 t/s) =====" >> $L
sh ./safe_run.sh $L ./llama-bench --kpu -m $M -t 1 -p 128 -n 16 -r 2
sync
