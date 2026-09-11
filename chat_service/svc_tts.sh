#!/bin/sh
# svc_tts.sh start|stop|status — 常驻 piper TTS daemon（模型常驻，FIFO 驱动）
# 协议：每行 <wav>\t<sid>\t<speed>\t<text>；回复行 READY / DONE <wav> <sec> <rtf>
# 注意：busybox pgrep/pkill -x 只比对 comm 前 15 字符（kpu_vits_runner_p 被截成
# kpu_vits_runner），写全名必然匹配失败 -> 重复 start 堆出多个 daemon 抢 FIFO。
. /mnt/data/chat/chat.conf
RLOG=$RUN/tts_out.log
DPID=$RUN/tts_daemon.pid

runner_up() {  # 当前日志里确有本代 daemon 且进程还活着
  [ -f $DPID ] || return 1
  kill -0 $(cat $DPID) 2>/dev/null || return 1
  grep -q READY $RLOG 2>/dev/null
}

case "$1" in
  start)
    mkdir -p $RUN
    if runner_up; then echo TTS_ALREADY; exit 0; fi
    # 清掉一切残留（老 daemon/holder/半死进程），再全新拉起
    pkill -x kpu_vits_runner 2>/dev/null
    pkill -f "sleep 10000000[0]" 2>/dev/null
    sleep 1
    # llama 读 700MB gguf 的 page cache 会钉死 CMA 区（mmz 要不出连续大块，
    # allocate_segment -1）；启动前丢页缓存保 MMZ 分配
    sync; echo 3 > /proc/sys/vm/drop_caches 2>/dev/null
    rm -f $RUN/tts_in $RLOG $DPID
    mkfifo $RUN/tts_in
    # FIFO 写端常驻持握，避免 runner 启动时 open 阻塞
    nohup sleep 100000000 < /dev/null > $RUN/tts_in 2>/dev/null &
    echo $! > $RUN/tts_holder.pid
    # TTS 与 llama 同优先级：nice 19 会被 llama 的 RT 线程饿死（实测重叠句
    # RTF 10~24，首句延迟几十秒）；同优先级下 decode -13~16%、TTS RTF~0.5-0.7，
    # 两头都保实时（用户拍板：尽量保 decode，TTS RTF<1 即可）
    nohup env PIPER_DP_KPU=$TTS_DP_KPU \
      PIPER_ENC_SFX=${TTS_ENC_SFX:-} PIPER_FLOW_SFX=${TTS_FLOW_SFX:-} PIPER_DEC_SFX=${TTS_DEC_SFX:-} \
      sh /mnt/data/kpu_llm/safe_run.sh $RLOG \
      $RUNNER --daemon --piper-dir=$TTS_DIR \
      < $RUN/tts_in > /dev/null 2>&1 &
    n=0
    while [ $n -lt 240 ]; do
      grep -q READY $RLOG 2>/dev/null && break
      sleep 0.5; n=$((n+1))
    done
    if grep -q READY $RLOG 2>/dev/null; then
      pgrep -x kpu_vits_runner > $DPID
      # 播放守护：llmd 流水送来的句子合成完就播（PCM Switch 开关压底噪）
      nohup sh /mnt/data/chat/player.sh > /dev/null 2>&1 &
      echo $! > $RUN/tts_player.pid
      echo TTS_READY
    else
      echo TTS_FAIL; tail -3 $RLOG 2>/dev/null; exit 1
    fi
    ;;
  stop)
    pkill -x kpu_vits_runner 2>/dev/null
    pkill -f "sleep 10000000[0]" 2>/dev/null
    [ -f $RUN/tts_player.pid ] && kill $(cat $RUN/tts_player.pid) 2>/dev/null
    pkill -f "player.s[h]" 2>/dev/null
    rm -f $DPID $RUN/tts_holder.pid $RUN/tts_player.pid
    echo TTS_STOPPED
    ;;
  status)
    if runner_up; then echo TTS_UP; else echo TTS_DOWN; fi
    ;;
  *) echo "usage: svc_tts.sh start|stop|status"; exit 2 ;;
esac
