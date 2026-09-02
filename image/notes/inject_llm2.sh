#!/bin/bash
set -e
IMG=/mnt/d/yilei.wang/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img
BIN=/mnt/d/yilei.wang/k230_prj/k230_llm/build-riscv/bin
R=/root/rootfs_clean.ext4

echo "== re-extract clean rootfs =="
dd if=$IMG of=$R bs=512 skip=262144 count=6508544 status=none

echo "== clean orphan files at / from failed attempt =="
debugfs -w -R "rm /llama-completion" $R 2>/dev/null || true
for f in llama-bench libllama-completion-impl.so libllama-bench-impl.so libllama-common.so.0 libllama.so.0 libggml.so.0 libggml-cpu.so.0 libggml-base.so.0 run_llm.sh; do
  debugfs -w -R "rm /$f" $R 2>/dev/null || true
done
debugfs -w -R "rmdir /root/llm" $R 2>/dev/null || true

cat > /tmp/run_llm.sh <<'EOF'
#!/bin/bash
export LD_LIBRARY_PATH=/root/llm
MODEL=${1:-/root/models/qwen3-q4km.gguf}
case "$2" in
  bench)
    exec /root/llm/llama-bench -m $MODEL -p 64 -n 32 ;;
  *)
    exec /root/llm/llama-completion -m $MODEL -co -no-cnv ;;
esac
EOF

echo "== build debugfs command file =="
cat > /tmp/dbgcmds <<'EOF'
mkdir /root/llm
cd /root/llm
write BINPATH/llama-completion llama-completion
sif llama-completion mode 0100755
write BINPATH/llama-bench llama-bench
sif llama-bench mode 0100755
write BINPATH/libllama-completion-impl.so libllama-completion-impl.so
write BINPATH/libllama-bench-impl.so libllama-bench-impl.so
write BINPATH/libllama-common.so.0.3.0 libllama-common.so.0
write BINPATH/libllama.so.0.3.0 libllama.so.0
write BINPATH/libggml.so.0.22.0 libggml.so.0
write BINPATH/libggml-cpu.so.0.22.0 libggml-cpu.so.0
write BINPATH/libggml-base.so.0.22.0 libggml-base.so.0
write /tmp/run_llm.sh run_llm.sh
sif run_llm.sh mode 0100755
EOF
sed -i "s#BINPATH#$BIN#g" /tmp/dbgcmds

echo "== inject via command file =="
debugfs -w -f /tmp/dbgcmds $R 2>&1 | grep -E 'Allocated|Invalid|Error' || true

echo "== verify /root/llm =="
debugfs -R "ls -l /root/llm" $R 2>/dev/null

echo "== verify / is clean =="
debugfs -R "ls /" $R 2>/dev/null | tr -s ' ' ',' | grep -E 'llama|run_llm' || echo "root clean OK"

echo "== write back =="
dd if=$R of=$IMG bs=512 seek=262144 conv=notrunc status=none
echo "ALL DONE"
