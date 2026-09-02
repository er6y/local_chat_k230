#!/bin/bash
export PAGER=cat
R=/root/mini_rootfs.ext4
echo "== [1] libc type & version =="
debugfs -R "ls -l /lib" $R 2>/dev/null | grep -E 'libc\.|ld-linux|ld-uclibc|ld-musl|libstdc|libgcc' | head -12
echo "-- /usr/lib too --"
debugfs -R "ls -l /usr/lib" $R 2>/dev/null | grep -E 'libc\.|ld-linux|libstdc|libgcc' | head -8
echo "== [2] S00resizemmc (sd device name + resize logic) =="
debugfs -R "cat /etc/init.d/S00resizemmc" $R 2>/dev/null
echo "== [3] S41adb_mtp (official gadget channel) =="
debugfs -R "cat /etc/init.d/S41adb_mtp" $R 2>/dev/null | head -30
echo "== [4] acm.sh content =="
debugfs -R "cat /root/script/acm.sh" $R 2>/dev/null
echo "== [5] fstab/inittab console =="
debugfs -R "cat /etc/inittab" $R 2>/dev/null | grep -v '^#' | head -8
echo "PROBE3 DONE"
