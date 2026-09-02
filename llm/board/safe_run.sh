#!/bin/sh
# safe_run.sh LOG CMD... -- watchdog-protected execution (v3):
# a feeder subshell owns /dev/watchdog for the whole run: opens the device,
# feeds every 8s while CMD runs, magic-closes (V) when /tmp/wdt_off appears
# or the parent dies. Open failure is NON-FATAL (run proceeds unprotected).
LOG="$1"; shift
rm -f /tmp/wdt_off
echo 3 > /proc/sys/vm/drop_caches 2>/dev/null   # reclaim CMA borrowed by page cache
echo V > /dev/watchdog 2>/dev/null              # plain command: disarm a stale claim if any
( echo "[safe_run] feeder spawn" >> "$LOG"
  exec 3<>/dev/watchdog 2>/dev/null             # busy -> subshell exits, unprotected run
  echo "[safe_run] wdt armed" >> "$LOG"
  while [ ! -e /tmp/wdt_off ] && kill -0 $PPID 2>/dev/null; do
    echo x >&3 2>/dev/null || exit 0
    sleep 8
  done
  echo V >&3 2>/dev/null
  exec 3>&-
) &
FEED=$!
"$@" >> "$LOG" 2>&1
RC=$?
touch /tmp/wdt_off
sleep 9                 # feeder wakes from its <=8s sleep, disarms and closes
wait $FEED 2>/dev/null
echo "[safe_run] done rc=$RC" >> "$LOG"
sync
exit $RC
