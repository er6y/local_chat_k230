#!/bin/sh
# e2e_perf.sh — 完整流水线性能测量：LLM→断句→TTS→aplay 播放
# 用法: sh e2e_perf.sh <问题1> <问题2> ...
D=/mnt/data/chat
RUN=/tmp/chat
n=0
for q in "$@"; do
  n=$((n+1))
  rm -f $RUN/t1_*_*.wav
  : > $RUN/aplay.log
  rm -f $RUN/.first_wav_$n $RUN/.play_$n
  t0=$(date +%s)
  ( while :; do
      [ -n "$(ls $RUN/t1_*_1.wav 2>/dev/null)" ] && { echo $(($(date +%s)-t0)) > $RUN/.first_wav_$n; break; }
      [ $(($(date +%s)-t0)) -gt 240 ] && break
      sleep 0.5
    done ) &
  ( while :; do
      grep -aq "Playing WAVE" $RUN/aplay.log 2>/dev/null && { echo $(($(date +%s)-t0)) > $RUN/.play_$n; break; }
      [ $(($(date +%s)-t0)) -gt 300 ] && break
      sleep 0.5
    done ) &
  sh $D/chat_demo.sh "$q" > $RUN/e2e_q$n.txt 2>&1
  t1=$(date +%s)
  nw=$(ls $RUN/t1_*_*.wav 2>/dev/null | wc -l)
  np=$(grep -ac "Playing WAVE" $RUN/aplay.log 2>/dev/null)
  nf=$(grep -ac "FAIL\|SKIP" $RUN/aplay.log 2>/dev/null)
  nb=$(grep -ac "DONE /tmp/chat/t1_" /tmp/chat/tts_out.log 2>/dev/null)
  echo "E2E q$n: total=$((t1-t0))s first_wav=$(cat $RUN/.first_wav_$n 2>/dev/null)s play_start=$(cat $RUN/.play_$n 2>/dev/null)s wavs=$nw played=$np fails=$nf"
done
echo E2E_DONE
