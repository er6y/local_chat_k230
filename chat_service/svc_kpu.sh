#!/bin/sh
# svc_kpu.sh start|stop|status — KPU GEMM 守护进程（静态全量预载，无动态加载）
# 持有全部 196 个 S16 era-B kmodel 解释器（~450MB CMA，drop_caches 后一次性
# 载入 + 逐个 warm-run ~25s），之后 serve 循环零加载、零驱逐（reject-not-evict）。
# 启动顺序硬约束：必须先于 svc_llm.sh 就绪——llmd 首次 mul_mat 才连 daemon
# 且只探测一次，连不上就永久降级 CPU prefill。
. /mnt/data/chat/chat.conf
KLOG=$RUN/kpu_out.log
DPID=$RUN/kpu_daemon.pid

kpu_up() {  # supervisor 活着 + daemon 已监听（KPUD_WARM=0 无 warmed 行）
  [ -f $DPID ] || return 1
  kill -0 $(cat $DPID) 2>/dev/null || return 1
  grep -q "listening on" $KLOG 2>/dev/null
}

case "$1" in
  start)
    mkdir -p $RUN
    if kpu_up; then echo KPU_ALREADY; exit 0; fi
    # 方括号防 pkill -f 自匹配；先停 supervisor 再停 python（否则会重生）
    [ -f $DPID ] && kill $(cat $DPID) 2>/dev/null
    pkill -f "kpu_gemm_daemo[n]" 2>/dev/null
    sleep 1
    # 页缓存会挤进 CMA 区不迁走（实测 ~320MB 假占用）：先 drop 才有完整池。
    # 再抬高 min_free_kbytes 保住 order:10 连续块——GNNE 驱动在 run 期做
    # 4MB GFP_KERNEL 连续分配，llm 的 700MB 页缓存把普通区碎片化后会分配
    # 失败，vendor runtime 不查错误直接段错误（2026-09-04 实录 rc=139）。
    sync; echo 3 > /proc/sys/vm/drop_caches 2>/dev/null
    echo 65536 > /proc/sys/vm/min_free_kbytes 2>/dev/null
    rm -f $KLOG $DPID
    # supervisor：daemon 万一段错误（GNNE 驱动级，无法在 python 内捕获）
    # 自动重生重新 warm（~25s）；期间客户端 CPU 回退不受影响，重生后
    # 客户端过 60s 毒窗自动重连。
    (
      while :; do
        echo "[svc] daemon spawn $(date)" >> $KLOG
        # KPUD_WARM=1：daemon 先全量加载、再 drop_caches、再统一试跑
        # （两段式，见 daemon warm 块）；min_free 提到 128MB 压 compaction
        # 保 order:10。KPUD_WARM=0 = 运行期惰性首跑（旧行为，调试用）。
        KPUD_CAP1=0 KPUD_CAP4=0 KPUD_CAP16=$KPU_CAP16 KPUD_WARM=1 \
        KPUD_IO_SCALE=${KPU_IO_SCALE:-1.0} \
          python3 /mnt/data/kpu_llm/kpu_gemm_daemon.py >> $KLOG 2>&1
        rc=$?
        echo "[svc] daemon exited rc=$rc $(date)" >> $KLOG
        # 崩溃死亡会泄漏已载 mmz 段：连续快速退出说明环境坏死了
        # （CMA 被泄漏吃光/区碎片化），疯重启只会越漏越多——长退避等人工
        if [ $rc -ne 0 ]; then
          fast=$((fast+1))
          [ $fast -ge 2 ] && sleep 3600
        else
          fast=0
        fi
        sleep 2
      done
    ) &
    echo $! > $DPID
    n=0
    while [ $n -lt 300 ]; do
      # 必须等 warmed：start 提前返回会让 TTS 与 daemon 的加载并发抢
      # I/O，warm 慢 17 倍且 zone 压力叠加（实录）
      grep -q warmed $KLOG 2>/dev/null && break
      if ! kill -0 $(cat $DPID 2>/dev/null) 2>/dev/null; then break; fi
      sleep 1; n=$((n+1))
    done
    if grep -q warmed $KLOG 2>/dev/null; then
      # warm 逐个读 kmodel 会在页缓存里留 ~230MB 副本（可迁移页落进 CMA
      # 空闲区，挤占 GNNE scratch 的连续块）——warm 完立刻丢弃
      sync; echo 3 > /proc/sys/vm/drop_caches 2>/dev/null
      grep warmed $KLOG | tail -1
      echo KPU_READY
    else
      echo KPU_FAIL; tail -3 $KLOG 2>/dev/null; exit 1
    fi
    ;;
  stop)
    # 先杀 supervisor（防重生），再 SIGTERM python（daemon 内 handler 做解释
    # 器释放）；SIGKILL 会把 mmz 段钉死在 CMA 里只能重启——万不得已才补刀。
    [ -f $DPID ] && kill $(cat $DPID) 2>/dev/null
    pkill -f "kpu_gemm_daemo[n]" 2>/dev/null
    n=0
    while [ $n -lt 6 ] && pgrep -f "kpu_gemm_daemo[n]" >/dev/null 2>&1; do
      sleep 0.5; n=$((n+1))
    done
    pgrep -f "kpu_gemm_daemo[n]" >/dev/null 2>&1 && pkill -9 -f "kpu_gemm_daemo[n]"
    rm -f $DPID /tmp/kpu_gemm.sock
    echo KPU_STOPPED
    ;;
  status)
    if kpu_up; then echo KPU_UP; else echo KPU_DOWN; fi
    ;;
  *) echo "usage: svc_kpu.sh start|stop|status"; exit 2 ;;
esac
