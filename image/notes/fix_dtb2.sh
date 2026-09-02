#!/bin/bash
# fix_dtb2.sh - remove sdhci,auto-cmd12 from sdhci0 in both DTBs (card stuck busy on auto-cmd12)
set -e
IMG="/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img"
P1=/root/mini_boot2.ext4

patch_dts() {
python3 - "$1" "$2" <<'EOF'
import re, sys
src_path, out_path = sys.argv[1], sys.argv[2]
src = open(src_path).read()
m = re.search(r'sdhci0@91580000 \{.*?\n\t\t\};', src, re.S)
if not m:
    print("FATAL: sdhci0 block not found"); sys.exit(1)
blk = m.group(0)
if '\t\t\tsdhci,auto-cmd12;\n' not in blk:
    print("FATAL: auto-cmd12 not found in sdhci0 (already removed?)"); sys.exit(1)
new = blk.replace('\t\t\tsdhci,auto-cmd12;\n', '')
src = src.replace(blk, new)
open(out_path, 'w').write(src)
print("patched: sdhci,auto-cmd12 removed from sdhci0")
EOF
}

echo "=== extract p1 from IMG ==="
dd if=$IMG of=$P1 bs=512 skip=61440 count=163840 status=none

for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    echo "=== $name ==="
    debugfs -R "dump /$name.dtb /root/$name.orig2.dtb" $P1 2>/dev/null
    dtc -I dtb -O dts -o /root/$name.dts /root/$name.orig2.dtb 2>/dev/null
    patch_dts /root/$name.dts /root/$name.patched2.dts
    dtc -I dts -O dtb -o /root/$name.new2.dtb /root/$name.patched2.dts 2>/dev/null
    # sanity checks: auto-cmd12 gone, 25MHz cap still present, highspeed still absent
    dtc -I dtb -O dts -o /root/$name.check2.dts /root/$name.new2.dtb 2>/dev/null
    grep -A 12 'sdhci0@91580000' /root/$name.check2.dts | grep -q 'auto-cmd12' && { echo "FATAL: auto-cmd12 still present"; exit 1; }
    grep -A 12 'sdhci0@91580000' /root/$name.check2.dts | grep -q 'max-frequency = <0x17d7840>' || { echo "FATAL: 25MHz cap missing"; exit 1; }
    grep -A 12 'sdhci0@91580000' /root/$name.check2.dts | grep -q 'cap-sd-highspeed' && { echo "FATAL: highspeed back"; exit 1; }
    echo "$name OK (auto-cmd12 removed, 25MHz kept)"
    debugfs -w -R "rm /$name.dtb" $P1 2>/dev/null
    debugfs -w -R "write /root/$name.new2.dtb /$name.dtb" $P1 2>/dev/null
done

echo "=== write p1 back ==="
dd if=$P1 of=$IMG bs=512 seek=61440 conv=notrunc status=none

echo "=== verify ==="
dd if=$IMG of=/root/p1_check2.ext4 bs=512 skip=61440 count=163840 status=none
for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    debugfs -R "dump /$name.dtb /root/$name.from_img2.dtb" /root/p1_check2.ext4 2>/dev/null
    cmp -s /root/$name.from_img2.dtb /root/$name.new2.dtb && echo "$name.dtb VERIFIED" || { echo "$name.dtb MISMATCH"; exit 1; }
done
echo "AUTO-CMD12 SURGERY DONE - IMG READY TO BURN"
