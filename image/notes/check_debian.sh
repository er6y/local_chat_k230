#!/bin/bash
# Check injected files inside debian image rootfs (read-only, no write back)
IMG=/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img
R=/root/rootfs_check.ext4
export PAGER=cat   # debugfs cat uses pager, disable it

if [ ! -f $R ]; then
  echo "== extracting rootfs partition (3.3GB, may take a while) =="
  dd if=$IMG of=$R bs=512 skip=262144 count=6508544 status=none
fi
echo "== extract done, size: $(stat -c%s $R) =="

echo ""
echo "===== [1] /usr/local/bin ====="
debugfs -R "ls -l /usr/local/bin" $R 2>/dev/null

echo ""
echo "===== [2] /etc/systemd/system/multi-user.target.wants ====="
debugfs -R "ls -l /etc/systemd/system/multi-user.target.wants" $R 2>/dev/null

echo ""
echo "===== [3] /etc/systemd/system/getty.target.wants ====="
debugfs -R "ls -l /etc/systemd/system/getty.target.wants" $R 2>/dev/null

echo ""
echo "===== [4] /root/llm ====="
debugfs -R "ls -l /root/llm" $R 2>/dev/null

echo ""
echo "===== [5] gadget.sh content ====="
debugfs -R "cat /usr/local/bin/gadget.sh" $R 2>/dev/null

echo ""
echo "===== [6] usb-gadget.service content ====="
debugfs -R "cat /etc/systemd/system/multi-user.target.wants/usb-gadget.service" $R 2>/dev/null

echo ""
echo "===== [7] agetty-ttyGS0.service content ====="
debugfs -R "cat /etc/systemd/system/getty.target.wants/agetty-ttyGS0.service" $R 2>/dev/null

echo ""
echo "===== [8] / root dir (check leftovers) ====="
debugfs -R "ls /" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -E 'tmp|gadget|service|llama' || echo "clean"

echo ""
echo "CHECK DONE"
