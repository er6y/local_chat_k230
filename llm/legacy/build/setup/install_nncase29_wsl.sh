#!/bin/bash
# Route A step-1 helper: install nncase 2.9 + nncase-kpu 2.9 into ~/venv_nncase29 (WSL)
# Retries because github.com connectivity is intermittent from this network.
set -u
VENV=~/venv_nncase29
PY=$VENV/bin/python
BASE=https://github.com/kendryte/nncase/releases/download/v2.9.0
W1=$BASE/nncase-2.9.0-cp310-cp310-manylinux_2_17_x86_64.manylinux2014_x86_64.whl
W2=$BASE/nncase_kpu-2.9.0-py2.py3-none-manylinux_2_17_x86_64.manylinux2014_x86_64.whl

$PY -m pip install --upgrade pip -q

for i in $(seq 1 30); do
  echo "== attempt $i =="
  if $PY -m pip install "$W1" "$W2"; then
    echo "INSTALL_OK"
    $PY -c "import nncase; print('nncase', nncase.__version__)"
    $PY -c "import _nncase_kpu; print('nncase_kpu native ok')" 2>/dev/null || $PY -m pip list 2>/dev/null | grep -i nncase
    exit 0
  fi
  sleep 10
done
echo "INSTALL_FAILED_ALL_ATTEMPTS"
exit 1
