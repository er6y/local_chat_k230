#!/bin/bash
# Probe mini linux v1.2 img: partition layout, rootfs free space, init system, ssh/gadget presence
export PAGER=cat
M=/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img
R=/root/mini_rootfs.ext4

echo "== [1] partition table =="
parted --script "$M" unit s print 2>&1 | tail -8

echo "== [2] extract rootfs =="
rm -f $R
START=$(parted --script "$M" unit s print 2>/dev/null | awk '/rootfs/{print $2}' | tr -d 's')
# fallback: use largest partition line
echo "rootfs start sector: $START"
dd if="$M" of=$R bs=512 skip=$START count=6508544 status=none || true
ls -la $R

echo "== [3] ext4 stats =="
debugfs -R "stats" $R 2>/dev/null | grep -E 'Block count|Free blocks|Block size|Filesystem features' | head -4

echo "== [4] root dirs =="
debugfs -R "ls /" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$' | head -30

echo "== [5] init =="
debugfs -R "cat /sbin/init" $R 2>/dev/null | head -3
debugfs -R "ls /etc/init.d" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$' | head -15

echo "== [6] ssh/gadget markers =="
debugfs -R "stat /usr/sbin/sshd" $R 2>/dev/null | head -2
debugfs -R "ls /root" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$' | head -15

echo "PROBE DONE"
