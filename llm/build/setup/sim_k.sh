#!/bin/bash
# sim_k.sh
export PATH=/usr/local/lib/python3.10/dist-packages:$PATH
cd /tmp/kpu_poc
python3 /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/sim_check_k.py 2>&1 | grep -v '^warn\|NNCASE_PLUGIN'
