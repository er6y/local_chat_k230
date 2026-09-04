#!/bin/bash
# build_tts.sh - cross-build tts_zh for big-core Linux (K230, xuantie g++)
# Produces tts/port/build-tts/tts_zh (static libstdc++/libgcc, dynamic glibc).
# Run in WSL. Requires /tmp/nncase_rt (nncase 2.11 runtime tgz extracted).
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"            # tts/
PORT="$ROOT/port"
V="$ROOT/vendor/buildroot-overlay/package/ai_demo"  # vendor tree
RT=/tmp/nncase_rt/nncase_k230_v2.11.0_runtime_linux
RTINC=/tmp/nncase_rt/include_root/src/Native/include
GSL=/tmp/nncase_rt/include_root/gsl_inc
TC="$HOME/xuantie/bin"

if [ ! -d "$RT" ] || [ ! -d "$RTINC" ]; then
    echo "FATAL: nncase 2.11 runtime ($RT) or full headers ($RTINC) missing."
    exit 1
fi

OUT="$ROOT/build-tts"
rm -rf "$OUT"
mkdir -p "$OUT"

CC="$TC/riscv64-unknown-linux-gnu-gcc"
CXX="$TC/riscv64-unknown-linux-gnu-g++"
MARCH="-march=rv64gcv_zfh_zvfh_zicbop_zihintpause"
CXXFLAGS="-std=c++17 -O2 $MARCH -fno-strict-aliasing"
CFLAGS="-std=gnu11 -O2 $MARCH -D_GNU_SOURCE -D_XOPEN_SOURCE=600"
INC="-I$RTINC -I$GSL -I$PORT -I$V/tts_zh/tts_src -I$V/tts_zh/include"

echo "== compile (C++) =="
for src in \
    "$PORT/main.cc" \
    "$PORT/ai_base.cc" \
    "$PORT/k230_init.cc" \
    "$PORT/play_pcm.cc" \
    "$V/tts_zh/tts_src/hifigan.cc" \
    "$V/tts_zh/tts_src/fastspeech1.cc" \
    "$V/tts_zh/tts_src/fastspeech2.cc" \
    "$V/tts_zh/tts_src/length_regulator.cpp" \
    $(ls "$V"/tts_zh/src/*.cpp)
do
    obj="$OUT/$(basename "${src%.*}").o"
    echo "  $src"
    $CXX $CXXFLAGS $INC -c "$src" -o "$obj"
done

echo "== compile (C) =="
$CC $CFLAGS $INC -c "$PORT/port_shim.c" -o "$OUT/port_shim.o"
# mmz_shim.c uses th.dcache.civa (T-Head CMO): needs xtheadcmo arch, same
# as the llama build compiles it with (second -march overrides the first).
$CC $CFLAGS -march=rv64gc_xtheadcmo $INC -c "$ROOT/../llm/build/mmz_shim.c" -o "$OUT/mmz_shim.o"

echo "== link =="
$CXX $CXXFLAGS \
    -o "$OUT/tts_zh" \
    "$OUT"/*.o \
    -Wl,--start-group \
    -Wl,--whole-archive "$RT/lib/libnncase.rt_modules.k230.a" -Wl,--no-whole-archive \
    "$RT/lib/libNncase.Runtime.Native.a" \
    "$RT/lib/libfunctional_k230.a" \
    -Wl,--end-group \
    -Wl,--wrap=gnne_enable -Wl,--wrap=gnne_ctrl_set -Wl,--wrap=gnne_disable \
    -static-libstdc++ -static-libgcc \
    -lpthread -ldl -lm

echo "== done =="
ls -la "$OUT/tts_zh"
"$TC/riscv64-unknown-linux-gnu-readelf" -d "$OUT/tts_zh" | grep -E "NEEDED" || true
