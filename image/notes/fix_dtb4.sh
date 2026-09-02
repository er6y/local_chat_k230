#!/bin/bash
# fix_dtb4.sh - full restore EXCEPT bus-width: 4-bit + 50MHz highspeed + auto-cmd12
# current img state: 1-bit + 25MHz + no-highspeed + no-auto-cmd12 (from fix_dtb/2/3)
set -e
IMG="/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img"
P1=/root/mini_boot4.ext4

patch_dts() {
python3 - "$1" "$2" <<'EOF'
import re, sys
src_path, out_path = sys.argv[1], sys.argv[2]
src = open(src_path).read()
m = re.search(r'sdhci0@91580000 \{.*?\n\t\t\};', src, re.S)
if not m:
    print("FATAL: sdhci0 block not found"); sys.exit(1)
blk = m.group(0)
if 'bus-width = <0x01>;' not in blk:
    print("FATAL: expected 1-bit bus-width, not found"); sys.exit(1)
if 'max-frequency = <0x17d7840>;' not in blk:
    print("FATAL: expected 25MHz max-frequency, not found"); sys.exit(1)
blk = blk.replace('bus-width = <0x01>;', 'bus-width = <0x04>;')
blk = blk.replace('max-frequency = <0x17d7840>;', 'max-frequency = <0x2faf080>;')
# re-add boolean props right after the bus-width line, reusing its indentation
mm = re.search(r'([ \t]*)bus-width = <0x04>;\n', blk)
ind = mm.group(1)
add = ind + 'cap-sd-highspeed;\n' + ind + 'sdhci,auto-cmd12;\n'
if 'cap-sd-highspeed' not in blk:
    blk = blk.replace(mm.group(0), mm.group(0) + add, 1)
src = src.replace(m.group(0), blk)
open(out_path, 'w').write(src)
print("patched: 4-bit + 50MHz + highspeed + auto-cmd12")
EOF
}

echo "=== extract p1 from IMG ==="
dd if=$IMG of=$P1 bs=512 skip=61440 count=163840 status=none

for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    echo "=== $name ==="
    debugfs -R "dump /$name.dtb /root/$name.orig4.dtb" $P1 2>/dev/null
    dtc -I dtb -O dts -o /root/$name.dts /root/$name.orig4.dtb 2>/dev/null
    patch_dts /root/$name.dts /root/$name.patched4.dts
    dtc -I dts -O dtb -o /root/$name.new4.dtb /root/$name.patched4.dts 2>/dev/null
    dtc -I dtb -O dts -o /root/$name.check4.dts /root/$name.new4.dtb 2>/dev/null
    C=$(grep -A 14 'sdhci0@91580000' /root/$name.check4.dts)
    echo "$C" | grep -q 'bus-width = <0x04>' || { echo "FATAL: 4-bit not set"; exit 1; }
    echo "$C" | grep -q 'max-frequency = <0x2faf080>' || { echo "FATAL: 50MHz not set"; exit 1; }
    echo "$C" | grep -q 'cap-sd-highspeed' || { echo "FATAL: highspeed missing"; exit 1; }
    echo "$C" | grep -q 'sdhci,auto-cmd12' || { echo "FATAL: auto-cmd12 missing"; exit 1; }
    echo "$C" | grep -q 'bus-width = <0x0[18]>' && { echo "FATAL: wrong bus width present"; exit 1; }
    echo "$name OK (4-bit bus, 50MHz, highspeed, auto-cmd12)"
    debugfs -w -R "rm /$name.dtb" $P1 2>/dev/null
    debugfs -w -R "write /root/$name.new4.dtb /$name.dtb" $P1 2>/dev/null
done

echo "=== write p1 back ==="
dd if=$P1 of=$IMG bs=512 seek=61440 conv=notrunc status=none

echo "=== verify ==="
dd if=$IMG of=/root/p1_check4.ext4 bs=512 skip=61440 count=163840 status=none
for name in k230-canmv-01studio k230-canmv-01studio-lcd; do
    debugfs -R "dump /$name.dtb /root/$name.from_img4.dtb" /root/p1_check4.ext4 2>/dev/null
    cmp -s /root/$name.from_img4.dtb /root/$name.new4.dtb && echo "$name.dtb VERIFIED" || { echo "$name.dtb MISMATCH"; exit 1; }
done

echo "=== export lcd dtb for on-board push ==="
cp /root/k230-canmv-01studio-lcd.new4.dtb /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/new_lcd4.dtb
md5sum /root/k230-canmv-01studio-lcd.new4.dtb /mnt/d/work/git_dev/k230_prj/k230_llm/.tools/new_lcd4.dtb
echo "4-BIT 50MHZ SURGERY DONE - IMG UPDATED, new_lcd4.dtb EXPORTED"
