#!/bin/bash
# fix_dtb3.sh - sdhci0 bus-width 8 -> 1 (last resort: 1-bit bus, bulletproof signal-wise)
set -e
IMG="/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img"
P1=/root/mini_boot3.ext4

patch_dts() {
python3 - "$1" "$2" <<'EOF'
import re, sys
src_path, out_path = sys.argv[1], sys.argv[2]
src = open(src_path).read()
m = re.search(r'sdhci0@91580000 \{.*?\n\t\t\};', src, re.S)
if not m:
    print("FATAL: sdhci0 block not found"); sys.exit(1)
blk = m.group(0)
if 'bus-width = <0x08>;' not in blk:
    print("FATAL: bus-width not found in sdhci0"); sys.exit(1)
new = blk.replace('bus-width = <0x08>;', 'bus-width = <0x01>;')
src = src.replace(blk, new)
open(out_path, 'w').write(src)
print("patched: sdhci0 bus-width 8 -> 1")
EOF
}

echo "=== extract p1 from IMG ==="
dd if=$IMG of=$P1 bs=512 skip=61440 count=163840 status=none

for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    echo "=== $name ==="
    debugfs -R "dump /$name.dtb /root/$name.orig3.dtb" $P1 2>/dev/null
    dtc -I dtb -O dts -o /root/$name.dts /root/$name.orig3.dtb 2>/dev/null
    patch_dts /root/$name.dts /root/$name.patched3.dts
    dtc -I dts -O dtb -o /root/$name.new3.dtb /root/$name.patched3.dts 2>/dev/null
    dtc -I dtb -O dts -o /root/$name.check3.dts /root/$name.new3.dtb 2>/dev/null
    grep -A 12 'sdhci0@91580000' /root/$name.check3.dts | grep -q 'bus-width = <0x01>' || { echo "FATAL: 1-bit not set"; exit 1; }
    grep -A 12 'sdhci0@91580000' /root/$name.check3.dts | grep -q 'max-frequency = <0x17d7840>' || { echo "FATAL: 25MHz cap missing"; exit 1; }
    grep -A 12 'sdhci0@91580000' /root/$name.check3.dts | grep -q 'auto-cmd12' && { echo "FATAL: auto-cmd12 back"; exit 1; }
    grep -A 12 'sdhci0@91580000' /root/$name.check3.dts | grep -q 'cap-sd-highspeed' && { echo "FATAL: highspeed back"; exit 1; }
    echo "$name OK (1-bit bus, 25MHz, no auto-cmd12, no highspeed)"
    debugfs -w -R "rm /$name.dtb" $P1 2>/dev/null
    debugfs -w -R "write /root/$name.new3.dtb /$name.dtb" $P1 2>/dev/null
done

echo "=== write p1 back ==="
dd if=$P1 of=$IMG bs=512 seek=61440 conv=notrunc status=none

echo "=== verify ==="
dd if=$IMG of=/root/p1_check3.ext4 bs=512 skip=61440 count=163840 status=none
for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    debugfs -R "dump /$name.dtb /root/$name.from_img3.dtb" /root/p1_check3.ext4 2>/dev/null
    cmp -s /root/$name.from_img3.dtb /root/$name.new3.dtb && echo "$name.dtb VERIFIED" || { echo "$name.dtb MISMATCH"; exit 1; }
done
echo "1-BIT BUS SURGERY DONE - IMG READY TO BURN"
