#!/bin/sh
# clean_bench.sh — 关掉 TTS/播放，做一次干净的 prefill/decode 测量
# 用法: sh /mnt/data/chat/clean_bench.sh [n_predict]
N=${1:-24}
pkill -x kpu_vits_runner 2>/dev/null
pkill -f "player.s[h]" 2>/dev/null
sleep 1
ask() {
  OUT=/tmp/chat/cb_out.txt
  LB=$(grep -c '^DONE' /tmp/chat/llm_out.log 2>/dev/null); [ -z "$LB" ] && LB=0
  printf '%s\t%s\t%s\n' "$OUT" "$N" "$1" > /tmp/chat/llm_in
  n=0
  while [ $n -lt 120 ]; do
    now=$(grep -c '^DONE' /tmp/chat/llm_out.log 2>/dev/null); [ -z "$now" ] && now=0
    [ "$now" -gt "$LB" ] && break
    sleep 1; n=$((n+1))
  done
}
echo "--- TTS off, n_predict=$N ---"
ask '用一句话介绍你自己'
ask '你叫什么名字'
grep PERF /tmp/chat/llm_out.log | tail -2
echo '--- CmaFree ---'; grep CmaFree /proc/meminfo
echo CLEAN_BENCH_DONE
