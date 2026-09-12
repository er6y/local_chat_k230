#!/bin/sh
# player.sh — 流水 TTS 播放守护：监听 tts_out.log 新增的 DONE 行，
# 按合成完成顺序 aplay 对应 wav 后删除。合成在 decode 期间进行，
# 这里只负责"好了就播"，多句按序排队。PCM Switch 播时开/播完关，
# 压掉推理期 DAC 底噪（用户实测耳机有电源干扰吱吱声）。
. /mnt/data/chat/chat.conf
LOG=$RUN/tts_out.log
[ -f "$LOG" ] || : > "$LOG"
n=$(grep -c '^DONE' "$LOG" 2>/dev/null); [ -z "$n" ] && n=0
while :; do
  now=$(grep -c '^DONE' "$LOG" 2>/dev/null); [ -z "$now" ] && now=0
  if [ "$now" -gt "$n" ]; then
    wav=$(grep '^DONE' "$LOG" | tail -n $((now - n)) | awk '{print $2}' | head -1)
    if [ -f "$wav" ]; then
      amixer cset name='PCM Switch' on >/dev/null 2>&1
      aplay "$wav" >/dev/null 2>&1
      amixer cset name='PCM Switch' off >/dev/null 2>&1
      rm -f "$wav"
    fi
    n=$((n + 1))
  elif [ "$now" -lt "$n" ]; then
    # tts 崩溃重启后 chatd 会清空 tts_out.log（防 stale READY），计数对齐回去
    n=$now
  else
    sleep 0.3
  fi
done
