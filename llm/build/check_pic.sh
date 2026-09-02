#!/bin/bash
# check_pic.sh - verify nncase static libs are PIC (needed to link into .so)
set -e
cd /tmp/nncase_rt/nncase_k230_v2.11.0_runtime_linux/lib
RE=/root/xuantie/bin/riscv64-unknown-linux-gnu-readelf
for A in libNncase.Runtime.Native.a libfunctional_k230.a; do
  echo "=== $A ==="
  OBJS=$(ar t $A | head -3)
  for O in $OBJS; do
    rm -f /tmp/pic_probe.o
    ar x $A $O && mv $O /tmp/pic_probe.o
    echo "-- $O relocs:"
    $RE -r /tmp/pic_probe.o | head -6
  done
done
