#!/bin/bash
# build_s4_all.sh - full 196 S=4 kmodels (M=16 tiles for decode path)
set -e
cd /mnt/d/work/git_dev/k230_prj/k230_llm/.tools
sed -e 's/^S = 16/S = 4/' \
    -e 's#^OUTDIR = "/tmp/kpu_poc/qwen"#OUTDIR = "/tmp/kpu_poc/qwen_s4"#' \
    make_qwen_kmodels.py > mk_s4_all.py
python3 mk_s4_all.py all --jobs 1 2>&1 | tail -3
echo "---- summary ----"
ls /tmp/kpu_poc/qwen_s4/*.kmodel | wc -l
du -sh /tmp/kpu_poc/qwen_s4/
