#!/bin/bash
# Round 4: modules.builtin - is UDC/gadget support built into debian kernel?
R=/root/rootfs_check.ext4
KVER=6.6.36
export PAGER=cat

echo "===== [A] modules.builtin exists? ====="
debugfs -R "ls -l /lib/modules/$KVER" $R 2>/dev/null | grep -E 'builtin|dep|alias'

echo ""
echo "===== [B] total builtin lines ====="
debugfs -R "cat /lib/modules/$KVER/modules.builtin" $R 2>/dev/null | wc -l

echo ""
echo "===== [C] usb-related builtin ====="
debugfs -R "cat /lib/modules/$KVER/modules.builtin" $R 2>/dev/null | grep -iE 'usb|udc|dwc|gadget|configfs|serial' | head -30

echo ""
echo "===== [D] configfs builtin? (fs dir) ====="
debugfs -R "ls /lib/modules/$KVER/kernel/fs" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | head -30

echo ""
echo "DIG4 DONE"
