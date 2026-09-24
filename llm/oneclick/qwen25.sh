#!/bin/bash
# qwen25.sh — Qwen2.5-0.5B-24L kv6_stacked 服务器一键：编译 → 推板 → 上板打分
#
# 用法（在 128 服务器 /ux/work/yilei.wang/k230/llm/oneclick/ 下）：
#   ./qwen25.sh all      # 编译→推板→打分（默认）
#   ./qwen25.sh compile  # 只编译（flock 串行）
#   ./qwen25.sh push     # 只推板（32MB 分块+md5）
#   ./qwen25.sh bench    # 只打分（reboot+golden window+poke+bd_kv6q2cpu）
#
# 判定基线（2026-09-24 定版）：
#   decode mean ≤ 289ms（基线 268ms + 8% 容差，268 = clean-data + NOC poke 后的现役值）
#   argmax 指纹 == 13（rng seed 9 固定输入，任何数值回归都会变）
#   参考系：官方同板 290ms（我们本就快 ~8%），官方宣称 202ms 本板未复现（见 llm/docs/optimization_space.md）
#
# 前置（服务器上应已就位，缺了会明确报错）：
#   /ux/work/yilei.wang/k230/qwen25_24l_s1h256_kv6_stacked.onnx   手术产物（WSL 侧生成）
#   /ux/work/yilei.wang/k230/calib48_diverse.npz                  校准样本
#   env/lib + dotnet-sdk + rebuild283_srv + conda                 编译环境
#   本脚本与 compile/ board/static/ 同在 llm/ 树内（repo scp 上来的）

set -uo pipefail

K=/ux/work/yilei.wang/k230
L=$K/llm
BOARD=root@172.16.72.255
BKM=/mnt/data/static/llm_kv6_stacked.kmodel
KM=$K/llm_kv6_stacked.kmodel
ONNX=$K/qwen25_24l_s1h256_kv6_stacked.onnx
CALIB=$K/calib48_diverse.npz
LOGDIR=$L/oneclick/logs
BASE_MS=268
TOL_PCT=8
FP=13
STAGE=${1:-all}
mkdir -p "$LOGDIR"
TAG=$(date +%m%d_%H%M)
LOG=$LOGDIR/qwen25_$TAG.log
exec >> "$LOG" 2>&1

die() { echo "[ONECLICK FAIL] $*"; exit 1; }

export_env() {
  export PYTHONPATH=$K/env/lib
  export DOTNET_ROOT=$K/dotnet-sdk
  export PATH=$K/dotnet-sdk:$PATH
  export NNCASE_PLUGIN_PATH=$K/rebuild283_srv
  export DOTNET_gcServer=0
  export OMP_NUM_THREADS=8 OPENBLAS_NUM_THREADS=8
}

do_compile() {
  [ -f "$ONNX" ] || die "缺 $ONNX —— 手术产物未生成（见 oneclick/README.md 手术段）"
  [ -f "$CALIB" ] || die "缺 $CALIB"
  [ -f "$L/compile/kv6_stacked.py" ] || die "缺 $L/compile/kv6_stacked.py（llm 树没 scp 全？）"
  OLD_MD5=$(md5sum "$KM" 2>/dev/null | cut -d' ' -f1 || true)
  echo "=== COMPILE START $(date) 旧md5=${OLD_MD5:-none} ==="
  export_env
  flock "$K/oneclick.lock" bash -c "
    cd $K
    $K/conda/bin/python $L/compile/kv6_stacked.py
  "
  rc=$?
  [ $rc -eq 0 ] || die "编译 rc=$rc"
  NEW_MD5=$(md5sum "$KM" | cut -d' ' -f1)
  echo "=== COMPILE DONE $(date) $(stat -c%s "$KM") B md5=$NEW_MD5 ==="
  if [ -n "${OLD_MD5:-}" ]; then
    if [ "$OLD_MD5" = "$NEW_MD5" ]; then
      echo "DETERMINISM: IDENTICAL to previous build ✓"
    else
      echo "DETERMINISM: CHANGED (old=$OLD_MD5 new=$NEW_MD5) —— 编译器/脚本被动过？"
    fi
  fi
}

do_push() {
  [ -f "$KM" ] || die "缺 $KM，先 compile"
  echo "=== PUSH START $(date) ==="
  bash "$L/compile/push_stacked.sh" || die "推板失败"
  BD_MD5=$(ssh -o BatchMode=yes $BOARD "md5sum $BKM" | cut -d' ' -f1)
  SRV_MD5=$(md5sum "$KM" | cut -d' ' -f1)
  [ "$BD_MD5" = "$SRV_MD5" ] || die "板上 md5 不一致 srv=$SRV_MD5 bd=$BD_MD5"
  echo "=== PUSH DONE md5 一致 $SRV_MD5 ==="
}

do_bench() {
  [ -f "$L/board/static/bd_kv6q2cpu.py" ] || die "缺 board/static/bd_kv6q2cpu.py"
  echo "=== BENCH START $(date) reboot ==="
  ssh -o BatchMode=yes $BOARD "reboot"
  sleep 125
  up=""
  for i in $(seq 1 12); do
    ssh -o BatchMode=yes -o ConnectTimeout=5 $BOARD "echo up" >/dev/null 2>&1 && { up=1; break; }
    sleep 10
  done
  [ -n "$up" ] || die "板子 4 分钟没起来"
  # golden window：开机满 230s 再动 CMA（过早 ~1GB 窗口装载会 segfault，见板端规程）
  sleep 105
  # 板端资产自举（幂等）：noc_poke.sh 曾缺失导致 bench 必死，推板脚本自带
  scp -q -o BatchMode=yes "$L/board/static/noc_poke.sh" $BOARD:/mnt/data/static/ || die "推 noc_poke.sh 失败"
  scp -q -o BatchMode=yes "$L/board/static/bd_kv6q2cpu.py" $BOARD:/mnt/data/static/ || die "推 bd_kv6q2cpu.py 失败"
  ssh -o BatchMode=yes $BOARD "sync; echo 3 > /proc/sys/vm/drop_caches; ln -sf /dev/k230-gnne /dev/gnne_device" || die "板端准备失败"
  ssh -o BatchMode=yes $BOARD "sh /mnt/data/static/noc_poke.sh" || die "noc_poke 失败"
  CMA=$(ssh -o BatchMode=yes $BOARD "grep CmaFree /proc/meminfo" | awk '{print \$2}')
  echo "CmaFree=${CMA}kB"
  [ "${CMA:-0}" -ge 550000 ] || { sleep 15; CMA=$(ssh -o BatchMode=yes $BOARD "grep CmaFree /proc/meminfo" | awk '{print \$2}'); }
  [ "${CMA:-0}" -ge 550000 ] || die "CmaFree 只有 ${CMA}kB（<550MB），窗口不对，重跑 bench"
  ssh -o BatchMode=yes $BOARD "python3 /mnt/data/static/bd_kv6q2cpu.py $BKM 20" > /tmp/oneclick_bd_$$.txt || die "bd_kv6q2cpu 失败"
  cat /tmp/oneclick_bd_$$.txt
  MEAN=$(grep -o 'mean=[0-9.]*' /tmp/oneclick_bd_$$.txt | cut -d= -f2)
  ARGMAX=$(grep -o 'argmax=[0-9-]*' /tmp/oneclick_bd_$$.txt | cut -d= -f2)
  rm -f /tmp/oneclick_bd_$$.txt
  [ -n "$MEAN" ] || die "没解析到 mean，看上面原始输出"
  LIMIT=$(awk "BEGIN{printf \"%d\", $BASE_MS * (1 + $TOL_PCT/100)}")
  echo ""
  echo "================ 判定 ================"
  echo "decode mean = ${MEAN}ms（基线 $BASE_MS，容差上限 $LIMIT）"
  echo "argmax 指纹 = ${ARGMAX}（期望 $FP）"
  PASS=OK
  awk -v m="$MEAN" -v l="$LIMIT" 'BEGIN{exit !(m<=l)}' || PASS=FAIL_SPEED
  [ "$ARGMAX" = "$FP" ] || PASS=FAIL_FINGERPRINT
  if [ "$PASS" = "OK" ]; then
    echo "RESULT: PASS ✓（对齐现役阶段）"
  else
    echo "RESULT: $PASS ✗ —— 查 llm/docs/known_issues.md"
  fi
  echo "======================================"
  echo "=== BENCH DONE $(date) ==="
}

echo "########## qwen25 oneclick [$STAGE] $(date) ##########"
case "$STAGE" in
  compile) do_compile ;;
  push)    do_push ;;
  bench)   do_bench ;;
  all)     do_compile; do_push; do_bench ;;
  *) die "用法: $0 [compile|push|bench|all]" ;;
esac
echo "########## oneclick [$STAGE] END rc=0 $(date) ##########"
