#!/bin/sh
# svc_llm.sh start|stop|status — 常驻 LLM daemon（llmd：模型+KV cache 常驻）
# 协议：每行 <out_path>\t<n_predict>\t<text>；回复 READY / DONE <out> <n> <sec> <tps>
# 注意：busybox pgrep/pkill -x 只比对 comm 前 15 字符（llmd 短，无此坑）
. /mnt/data/chat/chat.conf
LLOG=$RUN/llm_out.log
DPID=$RUN/llm_daemon.pid

daemon_up() {  # pidfile 活着 + 日志确有 READY
  [ -f $DPID ] || return 1
  kill -0 $(cat $DPID) 2>/dev/null || return 1
  grep -q READY $LLOG 2>/dev/null
}

case "$1" in
  start)
    mkdir -p $RUN
    if daemon_up; then echo LLM_ALREADY; exit 0; fi
    pkill -x llmd 2>/dev/null
    pkill -f "sleep 9999999[9]" 2>/dev/null
    sync; echo 3 > /proc/sys/vm/drop_caches 2>/dev/null
    for attempt in 1 2; do
      # 开机后首个 KPU 进程可能崩一次（gnne_regs /dev/mem），牺牲性重试
      rm -f $RUN/llm_in $LLOG $DPID
      mkfifo $RUN/llm_in
      # 2026-09-09：恢复单池 CMA 1024MB（DTB 双池实验已回退）后 daemon
      # 196/196 warm 稳定；llmd 由 KPU_ENABLE 开关决定是否 --kpu。
      # 实验结论存续：vendor runtime 分配失败不查错会段错误 + mmz 不可
      # 回收，daemon 存活时长仍是压测观察项。
      nohup sleep 99999999 < /dev/null > $RUN/llm_in 2>/dev/null &
      echo $! > $RUN/llm_holder.pid
      KPU_ENV=""; KPU_ARG=""
      if [ "$KPU_ENABLE" = "1" ]; then
      KPU_ENV=""; KPU_ARG=""
      if [ "$KPU_ENABLE" = "1" ]; then
        # KPU_DAEMON=1：tile 走 /tmp/kpu_gemm.sock（daemon 预热 196/196 三次
        # 验证稳定）。llmd 侧已带 DIRECT_IO（零页缓存零 CMA 占用）+ ubatch64，
        # 旧 serve 死因（309MB compute + 页缓存球铊碎 CMA）已除。
        KPU_ENV="KPU_DAEMON=1 KPU_HYBRID=1 KPU_MIN_M=32"
        KPU_ARG="--kpu"
      fi
      fi
      nohup env LD_LIBRARY_PATH=/mnt/data/kpu_llm LD_BIND_NOW=1 $KPU_ENV \
        LLMD_TTS_FIFO=$RUN/tts_in \
        LLMD_UBATCH=${LLMD_UBATCH:-256} \
        sh /mnt/data/kpu_llm/safe_run.sh $LLOG \
        $LLMD --model $LLM_MODEL $KPU_ARG --ctx-size $LLM_CTX --n-predict $LLM_PREDICT \
          --temp $LLM_TEMP --top-p $LLM_TOP_P --top-k $LLM_TOP_K \
        < $RUN/llm_in > /dev/null 2>&1 &
      n=0
      sleep 2   # 给 nohup->env->safe_run->exec 链一点启动时间，否则下面 pgrep
      # 上限 600s：drop_caches 后 Q4_K_M 冷加载（484MB mmap + 309MB compute
      # buffer + KPU 首连）实测 >180s，360×0.5s 的旧上限会在 READY 前一刻
      # 把 llmd 杀掉（2026-09-09 两次 rc=143 实录）。llmd 真死仍会提前 break。
      while [ $n -lt 1200 ]; do
        grep -q READY $LLOG 2>/dev/null && break
        if [ $n -gt 20 ] && ! pgrep -x llmd >/dev/null 2>&1; then break; fi
        sleep 0.5; n=$((n+1))
      done
      if grep -q READY $LLOG 2>/dev/null; then
        pgrep -x llmd > $DPID
        # llmd 权重是 anon 私有缓冲（fork 无 mmap 路径），加载用的 gguf 页
        # 缓存(~480MB)是纯废球铊：解码期回收风暴把它在 zone/CMA 间来回倒
        # （实测 CmaFree 锯齿 112~224MB），谷底碎片化咬死 daemon 的 GNNE
        # 连续分配。权重不读文件，drop 后不会长回来。
        sync; echo 3 > /proc/sys/vm/drop_caches 2>/dev/null
        echo LLM_READY
        exit 0
      fi
      echo "LLM_RETRY $attempt" >&2
      pkill -x llmd 2>/dev/null
      sleep 2
    done
    echo LLM_FAIL; tail -3 $LLOG 2>/dev/null; exit 1
    ;;
  stop)
    pkill -x llmd 2>/dev/null
    pkill -f "sleep 9999999[9]" 2>/dev/null
    rm -f $DPID $RUN/llm_holder.pid
    echo LLM_STOPPED
    ;;
  status)
    if daemon_up; then echo LLM_UP; else echo LLM_DOWN; fi
    ;;
  *) echo "usage: svc_llm.sh start|stop|status"; exit 2 ;;
esac
