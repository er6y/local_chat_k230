#!/bin/sh
# cpu_baseline.sh — 建立可信基线：KPU 全关（纯 CPU），验文本
# 关法要点：必须让客户端 manifest 为空（KPU_S16_DIR 指向不存在目录），
# 否则客户端会走"非 daemon 非 local"的 legacy 路径 —— 那条路会硬挂板子。
set -x
echo "=== 1) 改配置：KPU 全关 ==="
cd /mnt/data/chat
sed -i 's#^KPU_ENABLE=.*#KPU_ENABLE=0#' chat.conf
sed -i 's#^KPU_S16_DIR=.*#KPU_S16_DIR=/mnt/data/kpu_qwen/DISABLED_BY_CPU_BASELINE#' chat.conf
grep -E "KPU_ENABLE|KPU_S16_DIR|KPU_S16_MIN|KPU_CAP16|LLMD_UBATCH" chat.conf
sync

echo "=== 2) 停旧流水（等干净）==="
pkill -x llmd 2>/dev/null; pkill -x chatd 2>/dev/null; sleep 2
for i in $(seq 1 25); do pgrep -x kpud >/dev/null || break; sleep 1; done
pkill -9 -x kpud 2>/dev/null; pkill -9 -x kvr_new 2>/dev/null; sleep 1
rm -f /tmp/kpu_gemm.sock
pgrep -x kpud >/dev/null && echo "KPUD ALIVE(BAD)" || echo "no kpud (good)"
echo "chatd count: $(pgrep -c -x chatd)"

echo "=== 3) 起流水（记录基线行号）==="
: > /tmp/chat/chatd.log
/etc/init.d/S99chat start
BASE=$(wc -l < /tmp/chat/llm_out.log 2>/dev/null || echo 0)
echo "llm_out baseline lines=$BASE"
for i in $(seq 1 60); do
  if pgrep -x llmd >/dev/null; then
    NOW=$(wc -l < /tmp/chat/llm_out.log 2>/dev/null || echo 0)
    if [ "$NOW" -gt "$BASE" ] && tail -3 /tmp/chat/llm_out.log | grep -q "READY"; then
      echo "LLMD READY(fresh) ~${i}x3s"; break
    fi
  fi
  sleep 3
done
tail -3 /tmp/chat/llm_out.log
echo "kpud running? $(pgrep -c -x kpud) (应为 0)"

echo "=== 4) ask 两轮验文本 ==="
sh /mnt/data/chat/ask.sh "用一句话介绍你自己" 2>&1 | tail -2
sh /mnt/data/chat/ask.sh "我叫什么名字" 2>&1 | tail -2
echo "=== DONE $(date +%T) ==="
