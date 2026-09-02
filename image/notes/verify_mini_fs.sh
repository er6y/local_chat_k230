#!/bin/bash
export PAGER=cat
R=/root/mini_rootfs.ext4
echo "== fs real size =="
debugfs -R "stats" $R 2>&1 | grep -E 'Block count|Free blocks|Block size|Filesystem state'
echo "== dump model back =="
debugfs -R "dump /root/models/qwen3-q4km.gguf /root/model_back.gguf" $R 2>&1 | tail -1
md5sum /root/qwen3-q4km.gguf /root/model_back.gguf
echo "== dump llama-bench back =="
debugfs -R "dump /root/llm/llama-bench /root/bench_back" $R 2>&1 | tail -1
md5sum /root/stage_llm/llama-bench /root/bench_back
echo "== e2fsck quick =="
e2fsck -fn $R 2>&1 | tail -4
echo "VERIFY MINI DONE"
