#!/bin/bash
# sim29.sh — run C-model verification for nncase 2.9-built kmodels (venv python)
# The 2.9 Simulator spawns `nncase.simulator.k230.sc` by name, so venv
# site-packages must be on PATH (same trick sim_k.sh uses for the 2.11 install).
SITE=~/venv_nncase29/lib/python3.10/site-packages
cd /tmp/kpu_poc
PATH="$SITE:$PATH" ~/venv_nncase29/bin/python \
  /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/sim_check_29.py "$@" 2>&1 \
  | grep -v 'warn\|PluginLoader\|is not set'
