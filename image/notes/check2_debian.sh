#!/bin/bash
# Dig kernel modules & boot config from debian rootfs (read-only)
R=/root/rootfs_check.ext4
export PAGER=cat

KVER=$(debugfs -R "ls /lib/modules" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -E '^[0-9]+\.[0-9]+' | head -1)
echo "KVER=$KVER"

echo ""
echo "===== [A] /lib/modules/<ver>/kernel/drivers/usb/gadget ====="
debugfs -R "ls /lib/modules/$KVER/kernel/drivers/usb/gadget" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$'

echo ""
echo "===== [B] gadget/udc dir ====="
debugfs -R "ls /lib/modules/$KVER/kernel/drivers/usb/gadget/udc" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$'

echo ""
echo "===== [C] dwc3 anywhere under usb ====="
debugfs -R "ls /lib/modules/$KVER/kernel/drivers/usb" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -i dwc

echo ""
echo "===== [D] f_* function modules (legacy dir) ====="
debugfs -R "ls /lib/modules/$KVER/kernel/drivers/usb/gadget/legacy" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$'

echo ""
echo "===== [E] /etc/fstab ====="
debugfs -R "cat /etc/fstab" $R 2>/dev/null

echo ""
echo "===== [F] /etc/systemd/system all units ====="
debugfs -R "ls /etc/systemd/system" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^\.'

echo ""
echo "===== [G] resize/firstboot service? ====="
debugfs -R "ls /etc/systemd/system" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -iE 'resize|first|expand' || echo "none"

echo ""
echo "===== [H] modules-load.d ====="
debugfs -R "ls /etc/modules-load.d" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^\.'
debugfs -R "cat /etc/modules-load.d/modules.conf" $R 2>/dev/null || true

echo ""
echo "===== [I] network config (for ECM later) ====="
debugfs -R "ls /etc/network/interfaces.d" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^\.'

echo ""
echo "DIG DONE"
