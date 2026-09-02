#!/bin/sh
# cleanup_llama.sh - kill all llama-cli, drop caches, report memory
for p in $(ls /proc | grep -E '^[0-9]+$'); do
    c=$(cat /proc/$p/cmdline 2>/dev/null | tr '\0' ' ')
    case "$c" in *llama-cli*) kill -9 $p 2>/dev/null && echo "killed $p" ;; esac
done
sleep 2
echo 3 > /proc/sys/vm/drop_caches 2>/dev/null
grep -aE 'Cma|MemFree' /proc/meminfo
