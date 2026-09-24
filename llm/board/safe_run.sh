#!/bin/sh
# safe_run.sh LOG CMD... -- v4 (2026-09-14): WDT feeder REMOVED.
# Root cause proven: vendor /dev/watchdog keeps counting after unclean
# feeder close -> hardware bite ~30-60s later = SILENT SoC reset
# (verified: kill -9 feeder -> reset 54s later, serial shows zero kernel output).
# Hang protection now lives in chatd (180s llmd watchdog, targeted kill only).
LOG="$1"; shift
# drop_caches 撤销(2026-09-15):KPU=0 后页缓存无 CMA 冲突,AUTO 模式
# 靠它做 llmd 热重启(31s→~6s);kvr 的 CMA 需求内核会按需驱逐页缓存
echo 0 > /proc/sys/vm/drop_caches 2>/dev/null
"$@" >> "$LOG" 2>&1
RC=$?
sync
exit $RC
