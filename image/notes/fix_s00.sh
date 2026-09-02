#!/bin/bash
# fix_s00.sh - disable S00resizemmc in mini rootfs (it hammers the card AND would eat p3)
set -e
IMG="/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img"
MR=/root/mini_rootfs_work.ext4

echo "=== extract p2 (400MiB @ 262144s) from IMG ==="
dd if=$IMG of=$MR bs=512 skip=262144 count=819200 status=none
md5sum $MR

echo "=== inittab ==="
debugfs -R "cat /etc/inittab" $MR 2>/dev/null

echo "=== init.d listing ==="
debugfs -R "ls /etc/init.d" $MR 2>/dev/null

echo "=== remove S00resizemmc ==="
debugfs -w -R "rm /etc/init.d/S00resizemmc" $MR 2>/dev/null
echo "--- after removal ---"
debugfs -R "ls /etc/init.d" $MR 2>/dev/null | tr -s ' ' '\n' | grep -i s00 || echo "S00resizemmc GONE (no S00* left)"

echo "=== write rootfs back into IMG @ 128MiB ==="
dd if=$MR of=$IMG bs=512 seek=262144 conv=notrunc status=none

echo "=== verify: re-extract and compare ==="
dd if=$IMG of=/root/mini_rootfs_check.ext4 bs=512 skip=262144 count=819200 status=none
if cmp -s $MR /root/mini_rootfs_check.ext4; then echo "ROOTFS SEGMENT VERIFIED IN IMG"; else echo "MISMATCH"; exit 1; fi
md5sum /root/mini_rootfs_check.ext4
echo "S00 SURGERY DONE - IMG READY"
