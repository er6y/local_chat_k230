#!/bin/bash
# fix_xt_sysroot3.sh - add unversioned dev symlinks (-lpthread needs libpthread.so,
# -lm needs libm.so, ...) then rebuild llama-bench with the Xuantie toolchain.
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
S=$ROOT/llm/build/xuantie/sysroot
L=$S/lib64/lp64d
cd "$L"
ln -sf libpthread.so.0  libpthread.so
ln -sf libm.so.6        libm.so
ln -sf libdl.so.2       libdl.so
ln -sf librt.so.1       librt.so
ln -sf libresolv.so.2   libresolv.so
ln -sf libcrypt.so.1    libcrypt.so
ln -sf libnsl.so.1      libnsl.so
ln -sf libutil.so.1     libutil.so
ln -sf libanl.so.1      libanl.so
ln -sf libBrokenLocale.so.1 libBrokenLocale.so
ls -la libpthread.so libm.so
echo DEV_SYMLINKS_FIXED
bash "$ROOT/llm/build/build_xt.sh"
