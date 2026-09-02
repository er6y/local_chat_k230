#!/bin/bash
# Build mini linux v1.2 "full package": expand rootfs +512MB, transplant llama suite from debian rootfs, embed Qwen3 model
set -e
export PAGER=cat
M=/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img
MBAK=${M}.bak-premodel
D=/root/rootfs_big.ext4      # debian rootfs with /root/llm (source of transplant)
R=/root/mini_rootfs.ext4     # mini rootfs working copy (already extracted)
STAGE=/root/stage_llm
MODEL=/root/qwen3-q4km.gguf  # model staged earlier (462MB)

echo "== [1/9] backup mini img =="
if [ ! -f "$MBAK" ]; then cp "$M" "$MBAK"; fi
ls -la "$MBAK"

echo "== [2/9] dump llama suite from debian rootfs =="
rm -rf $STAGE; mkdir -p $STAGE
for f in llama-completion llama-bench libllama-completion-impl.so libllama-bench-impl.so libllama-common.so.0 libllama.so.0 libggml.so.0 libggml-cpu.so.0 libggml-base.so.0 run_llm.sh; do
    debugfs -R "dump /root/llm/$f $STAGE/$f" $D 2>/dev/null
done
ls -la $STAGE

echo "== [3/9] expand mini rootfs +512MB (1KB blocks: 409600 -> 934912) =="
truncate -s 957341696 $R
e2fsck -fp $R 2>&1 | tail -1 || true
resize2fs $R 2>&1 | tail -1
debugfs -R "stats" $R 2>/dev/null | grep -E 'Block count|Free blocks'

echo "== [4/9] inject llama suite + model into mini rootfs =="
cat > /tmp/miniinject <<'EOF'
mkdir /root/llm
cd /root/llm
write /root/stage_llm/llama-completion llama-completion
sif llama-completion mode 0100755
write /root/stage_llm/llama-bench llama-bench
sif llama-bench mode 0100755
write /root/stage_llm/libllama-completion-impl.so libllama-completion-impl.so
sif libllama-completion-impl.so mode 0100755
write /root/stage_llm/libllama-bench-impl.so libllama-bench-impl.so
sif libllama-bench-impl.so mode 0100755
write /root/stage_llm/libllama-common.so.0 libllama-common.so.0
sif libllama-common.so.0 mode 0100755
write /root/stage_llm/libllama.so.0 libllama.so.0
sif libllama.so.0 mode 0100755
write /root/stage_llm/libggml.so.0 libggml.so.0
sif libggml.so.0 mode 0100755
write /root/stage_llm/libggml-cpu.so.0 libggml-cpu.so.0
sif libggml-cpu.so.0 mode 0100755
write /root/stage_llm/libggml-base.so.0 libggml-base.so.0
sif libggml-base.so.0 mode 0100755
write /root/stage_llm/run_llm.sh run_llm.sh
sif run_llm.sh mode 0100755
mkdir /root/models
cd /root/models
write /root/qwen3-q4km.gguf qwen3-q4km.gguf
sif qwen3-q4km.gguf mode 0100644
EOF
debugfs -w -f /tmp/miniinject $R 2>&1 | grep -E 'Allocated|Error|Invalid|exists|not found' || true
echo "-- verify llm dir --"
debugfs -R "ls -l /root/llm" $R 2>/dev/null
echo "-- verify model --"
debugfs -R "ls -l /root/models" $R 2>/dev/null
debugfs -R "stats" $R 2>/dev/null | grep 'Free blocks'

echo "== [5/9] expand mini img to fit =="
RSZ=$(stat -c%s $R)
NEWIMG=$(( 262144*512 + RSZ + 1048576 ))   # rootfs + 1MB tail
truncate -s $NEWIMG "$M"
echo "img now: $(stat -c%s "$M") bytes, rootfs: $RSZ"

echo "== [6/9] fix GPT + grow rootfs partition to 100% =="
printf 'Fix\n' | parted ---pretend-input-tty "$M" print >/dev/null 2>&1 || true
parted --script "$M" unit s resizepart 2 100% 2>&1 | grep -v semaphore || true
parted --script "$M" unit s print 2>&1 | tail -5

echo "== [7/9] write rootfs back =="
dd if=$R of="$M" bs=1M seek=256 conv=notrunc status=progress 2>&1 | tail -1

echo "== [8/9] md5 verify =="
BACK=/root/mini_rootfs_back.ext4
rm -f $BACK
dd if="$M" of=$BACK bs=1M skip=256 count=$((RSZ/1048576 + 1)) status=none
truncate -s $RSZ $BACK
H1=$(md5sum $R | cut -d' ' -f1); H2=$(md5sum $BACK | cut -d' ' -f1)
echo "src: $H1"; echo "img: $H2"
[ "$H1" = "$H2" ] && echo "MD5 MATCH - MINI IMG READY (llama+model embedded)" || { echo "MISMATCH - restore bak!"; exit 2; }

echo "== [9/9] cleanup + summary =="
rm -f $BACK
ls -la "$M" "$MBAK"
echo "MINI INJECT ALL DONE"
