#!/bin/bash
set -e
R=/root/rootfs.ext4
BIN=/mnt/d/yilei.wang/k230_prj/k230_llm/build-riscv/bin
IMG=/mnt/d/yilei.wang/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img

echo "== mkdir /root/llm =="
debugfs -w -R "mkdir /root/llm" $R 2>/dev/null || true

inj() { # local file, remote name
  debugfs -w -R "write $1 $2" $R 2>/dev/null && echo "  + $2"
  debugfs -w -R "sif /root/llm/$2 mode 0100755" $R 2>/dev/null || true
}

echo "== inject binaries =="
inj $BIN/llama-completion llama-completion
inj $BIN/llama-bench llama-bench
inj $BIN/libllama-completion-impl.so libllama-completion-impl.so
inj $BIN/libllama-bench-impl.so libllama-bench-impl.so
inj $BIN/libllama-common.so.0.3.0 libllama-common.so.0
inj $BIN/libllama.so.0.3.0 libllama.so.0
inj $BIN/libggml.so.0.22.0 libggml.so.0
inj $BIN/libggml-cpu.so.0.22.0 libggml-cpu.so.0
inj $BIN/libggml-base.so.0.22.0 libggml-base.so.0

cat > /tmp/run_llm.sh <<'EOF'
#!/bin/bash
export LD_LIBRARY_PATH=/root/llm
MODEL=${1:-/root/models/qwen3-q4km.gguf}
case "$2" in
  bench)
    exec /root/llm/llama-bench -m $MODEL -p 64 -n 32 ;;
  chat|*)
    exec /root/llm/llama-completion -m $MODEL -co -no-cnv ;;
esac
EOF
inj /tmp/run_llm.sh run_llm.sh

echo "== verify injected =="
debugfs -R "ls -l /root/llm" $R 2>/dev/null

echo "== write back to img =="
dd if=$R of=$IMG bs=512 seek=262144 conv=notrunc status=none
echo "writeback done"
ls -la $IMG
