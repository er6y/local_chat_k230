#!/bin/bash
# Round 3: gadget function modules + kernel config evidence
R=/root/rootfs_check.ext4
KVER=6.6.36
export PAGER=cat

echo "===== [A] gadget/function modules ====="
debugfs -R "ls -l /lib/modules/$KVER/kernel/drivers/usb/gadget/function" $R 2>/dev/null

echo ""
echo "===== [B] modules.dep entries for usb_f_* ====="
debugfs -R "cat /lib/modules/$KVER/modules.dep" $R 2>/dev/null | grep -E 'usb_f_|libcomposite' | head -20

echo ""
echo "===== [C] /boot dir ====="
debugfs -R "ls -l /boot" $R 2>/dev/null | head -20

echo ""
echo "===== [D] interfaces.d filenames ====="
debugfs -R "ls -l /etc/network/interfaces.d" $R 2>/dev/null
debugfs -R "cat /etc/network/interfaces" $R 2>/dev/null | head -30

echo ""
echo "===== [E] sshd enabled? root pw ====="
debugfs -R "cat /etc/ssh/sshd_config" $R 2>/dev/null | grep -E '^(Port|PermitRootLogin|PasswordAuthentication)' | head -10

echo ""
echo "===== [F] usb-related builtins hint: /sys available? check modules.alias for udc ====="
debugfs -R "cat /lib/modules/$KVER/modules.alias" $R 2>/dev/null | grep -iE 'udc|dwc3' | head -10

echo ""
echo "DIG3 DONE"
