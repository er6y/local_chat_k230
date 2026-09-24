#!/bin/sh
cd /mnt/data/kpu_llm
rm -f /tmp/kpu_gemm.sock
nohup setsid python3 kpu_gemm_daemon.py > /mnt/data/kpwd_run.log 2>&1 < /dev/null &
sleep 12
pgrep -f kpu_gemm_daemon && echo DAEMON_UP || echo DAEMON_DEAD
python3 /mnt/data/kpu_llm/test_daemon.py
echo "=== daemon test done ==="
