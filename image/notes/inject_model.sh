#!/bin/bash
# Expand rootfs partition by 512MB and inject Qwen3-0.6B-Q4_K_M gguf into /root/models/
# All modifications are on: img copy -> rootfs_big.ext4 -> write back. Backup img first.
set -e
IMG=/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img
BAK=${IMG}.bak-premodel
SRC=/mnt/d/work/git_dev/k230_prj/k230_llm/models/qwen3_06b_gguf/Qwen3-0.6B-Q4_K_M.gguf
M=/root/qwen3-q4km.gguf
R=/root/rootfs_big.ext4
export PAGER=cat

ROOTFS_START_SECT=262144
NEW_ROOTFS_BYTES=3868245440            # 3584MB+512MB = 7555120 sectors
NEW_IMG_SECTORS=7819360                # 262144 + 7555120 + 4096 tail pad

echo "== [1/8] backup img =="
if [ ! -f $BAK ]; then cp $IMG $BAK; fi
ls -la $BAK

echo "== [2/8] stage model file locally =="
cp $SRC $M
ls -la $M

echo "== [3/8] stage rootfs copy =="
rm -f $R
cp /root/rootfs_check.ext4 $R

echo "== [4/8] expand ext4 file =="
truncate -s $NEW_ROOTFS_BYTES $R
e2fsck -fp $R || true
resize2fs $R 2>&1 | tail -2
debugfs -R "stats" $R 2>/dev/null | grep -E 'Block count|Free blocks'

echo "== [5/8] inject model into /root/models =="
cat > /tmp/dbgmodel <<'EOF'
mkdir /root/models
cd /root/models
write /root/qwen3-q4km.gguf qwen3-q4km.gguf
sif qwen3-q4km.gguf mode 0100644
EOF
debugfs -w -f /tmp/dbgmodel $R 2>&1 | grep -E 'Allocated|Error|Invalid|exists' || true
echo "-- verify --"
debugfs -R "ls -l /root/models" $R 2>/dev/null
debugfs -R "stats" $R 2>/dev/null | grep -E 'Free blocks'

echo "== [6/8] expand img + grow partition (GPT via parted) =="
truncate -s $((NEW_IMG_SECTORS*512)) $IMG
parted --script $IMG unit s resizepart 2 7817263s
parted --script $IMG unit s print | tail -6

echo "== [7/8] write rootfs back into img =="
dd if=$R of=$IMG bs=512 seek=$ROOTFS_START_SECT conv=notrunc status=none
ls -la $IMG

echo "== [8/8] relocate backup GPT if sgdisk available =="
if command -v sgdisk >/dev/null; then sgdisk -e $IMG && echo "sgdisk -e done"; else echo "no sgdisk, skip (primary GPT only)"; fi

echo "MODEL INJECT ALL DONE"
