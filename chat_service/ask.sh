#!/bin/sh
# ask.sh "问题" — 提问 → 流式打印回答文本；语音由流水 TTS 自动边生成边播出
# v2 (2026-09-14): 90s 无任何输出自动重发一次原问题——看门狗杀 llmd 重建时
# 排队在 FIFO 里的问题会随管道缓冲丢失，这是"第一问永远没回音"的根因；
# 重发后新 llmd 直接消费。副作用：若原问题只是慢（未丢），会连答两遍。
Q=$(echo "$*" | tr '\t' ' ')
OUT=/tmp/chat/ask_out.txt
[ -z "$Q" ] && { echo '用法: sh /mnt/data/chat/ask.sh 你的问题'; exit 1; }
LB=$(grep -c '^DONE' /tmp/chat/llm_out.log 2>/dev/null); [ -z "$LB" ] && LB=0
fsize() { if [ -f "$1" ]; then wc -c < "$1"; else echo 0; fi; }
: > /tmp/chat/ask_pending
printf '%s\t96\t%s\n' "$OUT" "$Q" > /tmp/chat/llm_in
echo '(思考中，边想边说：)'
n=0; shown=$(fsize $OUT); resent=0
while [ $n -lt 480 ]; do
  now=$(grep -c '^DONE' /tmp/chat/llm_out.log 2>/dev/null); [ -z "$now" ] && now=0
  sz=$(fsize $OUT)
  [ "$sz" -lt "$shown" ] && shown=0
  if [ "$sz" -gt "$shown" ]; then
    tail -c +$((shown+1)) $OUT 2>/dev/null
    shown=$sz
  fi
  [ "$now" -gt "$LB" ] && break
  if [ $n -ge 90 ] && [ $resent -eq 0 ]; then
    resent=1
    printf '%s\t96\t%s\n' "$OUT" "$Q" > /tmp/chat/llm_in
  fi
  sleep 1; n=$((n+1))
done
rm -f /tmp/chat/ask_pending
echo; echo
