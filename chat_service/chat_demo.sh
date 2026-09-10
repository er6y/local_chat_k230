#!/bin/sh
# chat_demo.sh [问题] — 流式对话 demo（纯 sh）
#   常驻 llmd 收 prompt（模型+KV cache 常驻，跨轮上下文连续）
#   -> 流式 token 落盘，demo 轮询增量，攒满一句（，。！？；：）即送常驻 TTS 队列
#   -> 顺序等每句 DONE 后 aplay（TTS 忙时句子在 FIFO/wav 排队，不乱序）
D=/mnt/data/chat
. $D/chat.conf
TLOG=$RUN/tts_out.log
LLOG=$RUN/llm_out.log
SENT_MAX=24         # 单轮最多朗读片段数（8 字小片，防 LLM 抽风塞爆 TTS）
QUEUE_MAX=4         # 未合成句数上限（超出则暂停派发，等 TTS 追上）
turn_no=0
mkdir -p $RUN

wait_done() {  # wait_done <wav> <timeout_s>
  w="$1"; t="${2:-90}"; j=0
  while [ $j -lt $t ]; do
    sleep 0.5; j=$((j+1))
    grep -aq "DONE $w" $TLOG 2>/dev/null && return 0
  done
  return 1
}

# one_stream <question>
one_stream() {
  q="$1"
  turn_no=$((turn_no+1))
  tag="t${turn_no}_$$"
  rm -f $RUN/${tag}_*.wav
  out=$RUN/gen.$tag.txt; : > $out
  pend=$RUN/pend.$$; : > $pend
  sent=0; pofs=0
  printf '小K: '
  # 发给常驻 llmd；结束标志 = LLOG 出现本轮 DONE（token 流式写 $out）
  printf '%s\t%s\t%s\n' "$out" "$LLM_PREDICT" "$q" > $RUN/llm_in
  i=0
  while ! grep -aq "DONE $out" $LLOG 2>/dev/null && [ $i -lt 240 ]; do
    i=$((i+1)); sleep 1
    sz1=$(wc -c < $out 2>/dev/null || echo 0)
    if [ "$sz1" -gt "$pofs" ]; then
      tail -c +$((pofs+1)) $out >> $pend
      pofs=$sz1
    fi
    clean=$(tr -d '\000-\010\013\014\016-\037' < $pend \
            | sed -e 's/\[[0-9;]*[a-zA-Z]//g' -e 's/<think>[^<]*<\/think>//g' \
                  -e 's/<\/think>//g' -e 's/<think>//g' \
            | tr -s ' \t' '  ')
    lines=$(printf '%s' "$clean" \
      | awk '{ gsub(/，/, "，\n"); gsub(/。/, "。\n"); gsub(/！/, "！\n");
              gsub(/？/, "？\n"); gsub(/；/, "；\n"); gsub(/：/, "：\n");
              gsub(/\n\n/, "\n"); printf "%s", $0 }')
    total=$(printf '%s\n' "$lines" | grep -c .)
    while :; do   # 派发 complete 句，队列超限就等
      done_cnt=$(grep -ac "DONE $RUN/${tag}_" $TLOG 2>/dev/null) || done_cnt=0
      [ $((sent - done_cnt)) -ge $QUEUE_MAX ] && { sleep 1; continue; }
      break
    done
    if [ "$total" -gt "$sent" ] && [ "$total" -gt 1 ]; then
      k=$sent
      while [ $k -lt $((total-1)) ] && [ $k -lt $SENT_MAX ]; do
        k=$((k+1))
        s=$(printf '%s\n' "$lines" | sed -n "${k}p" | tr -d '\n')
        [ -z "$(printf '%s' "$s" | tr -d ' ')" ] && continue
        sent=$k
        wav=$RUN/${tag}_${sent}.wav
        printf '%s\t1\t%s\t%s\n' "$wav" "$TTS_SPEED" "$s" > $RUN/tts_in
        printf '%s' "$s"
      done
    fi
  done
  # 收尾：最后一段也送掉
  if [ $sent -lt $SENT_MAX ]; then
    last=$(printf '%s' "$clean" \
      | awk '{ gsub(/，/, "，\n"); gsub(/。/, "。\n"); gsub(/！/, "！\n");
              gsub(/？/, "？\n"); gsub(/；/, "；\n"); gsub(/：/, "：\n");
              printf "%s", $0 }' \
      | sed -n "$((sent+1))p" | tr -d '\n')
    if [ -n "$(printf '%s' "$last" | tr -d ' ')" ]; then
      sent=$((sent+1))
      wav=$RUN/${tag}_${sent}.wav
      printf '%s\t1\t%s\t%s\n' "$wav" "$TTS_SPEED" "$last" > $RUN/tts_in
      printf '%s' "$last"
    fi
  fi
  echo ""
  rm -f $pend $out
  # 顺序播放；wav 保留不删（排查"吃词"用）；aplay 结果记日志
  k=0
  while [ $k -lt $sent ]; do
    k=$((k+1))
    wav=$RUN/${tag}_${k}.wav
    wait_done "$wav" 90 || { echo "[play] SKIP(timeout) $wav" >> $RUN/aplay.log; continue; }
    try=0
    while :; do
      aplay $wav >> $RUN/aplay.log 2>&1 && break
      try=$((try+1))
      echo "[play] FAIL try=$try $(date +%T) $wav" >> $RUN/aplay.log
      [ $try -ge 3 ] && break
      sleep 0.3
    done
  done
}

sh $D/svc_tts.sh status | grep -q TTS_UP || sh $D/svc_tts.sh start || exit 1
sh $D/svc_llm.sh status | grep -q LLM_UP || sh $D/svc_llm.sh start || exit 1

if [ $# -ge 1 ]; then
  one_stream "$*"
  exit 0
fi

echo '流式对话 demo（多轮上下文连续，quit 退出）'
while :; do
  printf '你> '
  read line || break
  [ -z "$line" ] && continue
  case "$line" in
    quit|exit|q) break ;;
  esac
  one_stream "$line"
done
