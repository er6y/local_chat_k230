#!/bin/sh
# clean_llama.sh - stop stray llama-cli instances (safe: pattern only matches llama)
for p in /proc/[0-9]*; do
    pid=${p#/proc/}
    if grep -qa llama-cli "$p/comm" 2>/dev/null; then
        kill -9 "$pid" 2>/dev/null
        echo "killed $pid"
    fi
done
