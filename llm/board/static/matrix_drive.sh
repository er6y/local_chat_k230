#!/bin/bash
# matrix_drive.sh — 官方模型 decode H 扫描驱动（每 H 一次 reboot，CMA 协议）
# 跑在有 ssh 到板子的机器上。用法：bash matrix_drive.sh "12 29 63 255"
set -uo pipefail
BOARD=root@172.16.72.255
for H in $1; do
  echo "########## H=$H $(date +%T) ##########"
  ssh -o BatchMode=yes $BOARD "reboot"
  sleep 125
  up=""
  for i in $(seq 1 12); do ssh -o BatchMode=yes -o ConnectTimeout=5 $BOARD "echo up" >/dev/null 2>&1 && { up=1; break; }; sleep 10; done
  [ -n "$up" ] || { echo "BOOT_FAIL H=$H"; continue; }
  sleep 105   # 开机满 230s
  ssh -o BatchMode=yes $BOARD "sync; echo 3 > /proc/sys/vm/drop_caches; ln -sf /dev/k230-gnne /dev/gnne_device; sh /mnt/data/static/noc_poke.sh" || { echo "PREP_FAIL H=$H"; continue; }
  ssh -o BatchMode=yes $BOARD "python3 /mnt/data/static/bench_official_matrix.py $H" || echo "RUN_FAIL H=$H"
done
echo "MATRIX_DRIVE_ALL_DONE"
