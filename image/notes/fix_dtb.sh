#!/bin/bash
# fix_dtb.sh - patch both DTBs in mini boot partition: cap SD at 25MHz (drop highspeed)
# then write patched boot partition back into the IMG and verify
set -e
IMG="/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img"
P1=/root/mini_boot.ext4

patch_dts() {
python3 - "$1" "$2" <<'EOF'
import re, sys
src_path, out_path = sys.argv[1], sys.argv[2]
src = open(src_path).read()
m = re.search(r'sdhci0@91580000 \{.*?\n\t\t\};', src, re.S)
if not m:
    print("FATAL: sdhci0 block not found"); sys.exit(1)
blk = m.group(0)
new = blk.replace('max-frequency = <0xbebc200>;', 'max-frequency = <0x17d7840>;')
if 'cap-sd-highspeed;' in new:
    new = new.replace('\t\t\tcap-sd-highspeed;\n', '')
else:
    print("FATAL: cap-sd-highspeed not found in sdhci0"); sys.exit(1)
src = src.replace(blk, new)
open(out_path, 'w').write(src)
print("patched sdhci0 in " + src_path + ": max-frequency=25MHz, cap-sd-highspeed removed")
EOF
}

for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    echo "=== $name ==="
    debugfs -R "dump /$name.dtb /root/$name.orig.dtb" $P1 2>/dev/null
    dtc -I dtb -O dts -o /root/$name.dts /root/$name.orig.dtb 2>/dev/null
    patch_dts /root/$name.dts /root/$name.patched.dts
    dtc -I dts -O dtb -o /root/$name.new.dtb /root/$name.patched.dts 2>/dev/null
    # sanity: confirm the new dtb carries the patch
    dtc -I dtb -O dts -o /root/$name.check.dts /root/$name.new.dtb 2>/dev/null
    grep -A 12 'sdhci0@91580000' /root/$name.check.dts | grep -E 'max-frequency|highspeed' || true
    grep -A 12 'sdhci0@91580000' /root/$name.check.dts | grep -q 'max-frequency = <0x17d7840>' || { echo "FATAL: patch not in compiled dtb"; exit 1; }
    grep -A 12 'sdhci0@91580000' /root/$name.check.dts | grep -q 'cap-sd-highspeed' && { echo "FATAL: highspeed still present"; exit 1; }
    echo "size: $(stat -c%s /root/$name.orig.dtb) -> $(stat -c%s /root/$name.new.dtb)"
    # replace file inside ext4
    debugfs -w -R "rm /$name.dtb" $P1 2>/dev/null
    debugfs -w -R "write /root/$name.new.dtb /$name.dtb" $P1 2>/dev/null
done

echo "=== write p1 back into IMG ==="
dd if=$P1 of=$IMG bs=512 seek=61440 conv=notrunc status=none

echo "=== verify: extract p1 from IMG, dump dtb, compare ==="
dd if=$IMG of=/root/p1_check.ext4 bs=512 skip=61440 count=163840 status=none
for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    debugfs -R "dump /$name.dtb /root/$name.from_img.dtb" /root/p1_check.ext4 2>/dev/null
    if cmp -s /root/$name.from_img.dtb /root/$name.new.dtb; then
        echo "$name.dtb: VERIFIED in IMG"
    else
        echo "$name.dtb: MISMATCH"; exit 1
    fi
done

echo "=== final: list boot partition ==="
debugfs -R "ls -l /" /root/p1_check.ext4 2>/dev/null
echo "DTB PATCH DONE - IMG READY TO BURN"
