#!/bin/bash
# push_fix.sh — 把 kpud + libggml-cpu.so 推到板子并校验（WSL 侧执行）
set -e
ROOT=/mnt/d/work/git_dev/k230_prj/local_chat_k230
BIN="$ROOT/build-riscv-kpu/bin"
cd "$BIN"
rm -f kpud_new.gz ggmlcpu_new.gz
gzip -c kpud > kpud_new.gz
gzip -c libggml-cpu.so.0.22.0 > ggmlcpu_new.gz
ls -la kpud_new.gz ggmlcpu_new.gz
md5sum kpud libggml-cpu.so.0.22.0 | tee /tmp/push_md5.txt
echo PUSH_PREP_OK
