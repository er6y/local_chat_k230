#!/bin/bash
# Probe2 mini linux: gadget modules availability; list llama files in debian rootfs for transplant
export PAGER=cat
R=/root/mini_rootfs.ext4
D=/root/rootfs_big.ext4

echo "== [1] mini linux kernel modules dir =="
debugfs -R "ls -l /lib/modules" $R 2>/dev/null | head -8
KVER=$(debugfs -R "ls /lib/modules" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$' | grep -v -E '^(\.|\.\.|[0-9]+\s)' | head -1)
# robust: take second field style
KVER=$(debugfs -R "ls /lib/modules" $R 2>/dev/null | awk '{for(i=1;i<=NF;i++) if($i ~ /^[0-9]+\./) print $i}' | head -1)
echo "kernel version dir: $KVER"

echo "== [2] gadget-related modules =="
for m in libcomposite.ko usb_f_acm.ko usb_f_rndis.ko u_serial.ko u_ether.ko configfs.ko; do
    FOUND=$(debugfs -R "ls /lib/modules/$KVER" $R 2>/dev/null | grep -c "$m" || true)
    echo "  $m : dir-match=$FOUND"
done
echo "-- search builtin (modules.builtin) --"
debugfs -R "cat /lib/modules/$KVER/modules.builtin" $R 2>/dev/null | grep -E 'udc|dwc2|usbcore|configfs|libcomposite' || echo "  (no builtin entries matched or file missing)"

echo "== [3] modules.dep gadget lines =="
debugfs -R "cat /lib/modules/$KVER/modules.dep" $R 2>/dev/null | grep -E 'libcomposite|usb_f_acm|usb_f_rndis|u_serial|u_ether' || echo "  (none in modules.dep)"

echo "== [4] kernel version string =="
debugfs -R "cat /etc/os-release" $R 2>/dev/null | head -3
debugfs -R "ls /lib/modules/$KVER/kernel" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$' | head -12

echo "== [5] debian /root/llm inventory (for transplant) =="
debugfs -R "ls -l /root/llm" $D 2>/dev/null

echo "== [6] mini /etc/init.d full list =="
debugfs -R "ls /etc/init.d" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$'

echo "== [7] mini /app and /root/script (deploy targets?) =="
debugfs -R "ls /app" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$' | head -10
debugfs -R "ls /root/script" $R 2>/dev/null | tr -s ' ' | tr ' ' '\n' | grep -v '^$' | head -10

echo "PROBE2 DONE"
