#!/bin/bash
# Check rootfs free space before injecting the GGUF model
R=/root/rootfs_check.ext4
export PAGER=cat
echo "== ext4 stats =="
debugfs -R "stats" $R 2>/dev/null | grep -E 'Block count|Free blocks|Block size|Filesystem volume'
echo ""
echo "== /root dir =="
debugfs -R "ls /root" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$'
echo ""
echo "== /root/llm/run_llm.sh content (model path) =="
debugfs -R "cat /root/llm/run_llm.sh" $R 2>/dev/null
echo ""
STATS DONE
