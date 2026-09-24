#!/bin/bash
# sim_k.sh - run nncase K230 simulator (2.11) for kmodel numeric check
export PATH=/usr/local/lib/python3.10/dist-packages:$PATH
cd /tmp/kpu_poc
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
python3 "$SCRIPT_DIR/sim_check_k.py" 2>&1 | grep -v '^warn\|NNCASE_PLUGIN'
