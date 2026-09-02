#!/bin/bash
# Resume inject_model.sh from step 6: fix GPT after truncate, grow rootfs partition, write rootfs back, full md5 verify
set -e
IMG=/mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img
R=/root/rootfs_big.ext4
ROOTFS_START_MB=256     # 262144 sectors * 512 = 128MiB = 256 MiB

IMGSZ=$(stat -c%s "$IMG")
echo "img actual size: $IMGSZ bytes ($((IMGSZ/512)) sectors)"
RSZ=$(stat -c%s $R)
echo "rootfs_big size: $RSZ bytes"

echo "== [6b] fix GPT backup header to new disk end =="
printf 'Fix\n' | parted ---pretend-input-tty "$IMG" print 2>&1 | tail -10 || true

echo "== [6c] grow rootfs partition to 100% =="
if ! parted --script "$IMG" unit s resizepart 2 100% 2>&1; then
    echo "-- script mode refused, retry feeding answers --"
    ENDSEC=$((IMGSZ/512 - 34 - 1))
    printf 'Yes\n'$ENDSEC's\nFix\n' | parted ---pretend-input-tty "$IMG" unit s resizepart 2 "$ENDSEC"s 2>&1 || true
fi

echo "== [6d] partition table now =="
parted --script "$IMG" unit s print 2>&1 | tail -8

echo "== [7] write rootfs back into img (3.6GB, ~1-2min) =="
dd if=$R of="$IMG" bs=1M seek=$ROOTFS_START_MB conv=notrunc status=progress 2>&1 | tail -1

echo "== [8] verify: extract rootfs region from img, full md5 compare =="
IMG_ROOTFS=/root/rootfs_from_img.ext4
rm -f $IMG_ROOTFS
dd if="$IMG" of=$IMG_ROOTFS bs=1M skip=$ROOTFS_START_MB count=$((RSZ/1048576 + 1)) status=none
truncate -s $RSZ $IMG_ROOTFS
H1=$(md5sum $R | cut -d' ' -f1)
H2=$(md5sum $IMG_ROOTFS | cut -d' ' -f1)
echo "src md5 : $H1"
echo "img md5 : $H2"
if [ "$H1" = "$H2" ]; then echo "MD5 MATCH - DEBIAN IMG READY (model embedded)"; else echo "MD5 MISMATCH - restore .bak-premodel manually!"; exit 2; fi

echo "== [9] decompress mini linux v1.2 (plan B base) =="
gunzip -k -f /mnt/d/work/git_dev/k230_prj/k230_llm/downloads/06_images/linux/CanMV-K230_01studio_linux_v1.2_nncase_v2.11.0.img.gz 2>&1 || true
ls -la /root/*.img 2>/dev/null
echo "FINISH ALL DONE"
