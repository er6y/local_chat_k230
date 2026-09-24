#!/bin/bash
# deploy_clean.sh - package and note the clean-calibration kmodels (board push
# happens from Windows after this)
set -e
cd /tmp/kpu_poc
echo "== S4 clean =="
ls qwen_s4sq_clean/*.kmodel | wc -l
find qwen_s4sq_clean -name '*.kmodel' -size -1k | wc -l
echo "== S16 clean =="
ls qwen_s16sq_clean/*.kmodel | wc -l
find qwen_s16sq_clean -name '*.kmodel' -size -1k | wc -l
du -sh qwen_s4sq_clean qwen_s16sq_clean
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUTDIR="$ROOT/../tmp/sq29_push"
mkdir -p "$OUTDIR"
tar cf "$OUTDIR/clean_kmodels.tar" qwen_s4sq_clean qwen_s16sq_clean
ls -la "$OUTDIR/clean_kmodels.tar"
echo PACKAGE_DONE
