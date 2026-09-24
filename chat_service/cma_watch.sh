#!/bin/sh
# cma_watch.sh — 采 CmaFree 抖动（诊断每次 KPU invoke 是否触发 CMA 分配/迁移）
# 用法: sh /mnt/data/chat/cma_watch.sh <seconds>
S=${1:-30}
i=0
n=$((S * 5))
min=999999999
max=0
prev=
while [ $i -lt $n ]; do
  v=$(while read -r line; do
        case "$line" in CmaFree:*) echo "${line#CmaFree:}" | tr -d ' kB'; break;; esac
      done < /proc/meminfo)
  [ -z "$v" ] && v=0
  [ "$v" -lt "$min" ] && min=$v
  [ "$v" -gt "$max" ] && max=$v
  if [ -n "$prev" ] && [ "$v" != "$prev" ]; then
    d=$((v - prev))
    [ ${d#-} -gt 2048 ] && echo "jump ${d} KB -> $v KB"
  fi
  prev=$v
  i=$((i + 1))
  sleep 0.2
done
echo "CMA min=$min KB max=$max KB spread=$((max - min)) KB"
