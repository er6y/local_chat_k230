#!/bin/bash
set -e
export PATH=$HOME/xuantie/bin:$PATH

ROOT=/mnt/d/work/git_dev/k230_prj/local_chat_k230
PORT=$ROOT/tts/port
RT=/tmp/nncase_rt/nncase_k230_v2.11.0_runtime_linux
RTINC=/tmp/nncase_rt/include_root/src/Native/include
GSL=/tmp/nncase_rt/include_root/gsl_inc
ORTINC=/tmp/sherpa-onnx/build-riscv64/_deps/onnxruntime-src/include
SHERPA_SRC=/tmp/sherpa-onnx
SHERPA_BUILD=/tmp/sherpa-onnx/build-riscv64

OUT=$ROOT/build-tts
mkdir -p $OUT

CC=riscv64-unknown-linux-gnu-gcc
CXX=riscv64-unknown-linux-gnu-g++
MARCH="-march=rv64gcv_zfh_zvfh_zicbop_zihintpause"
CXXFLAGS="-std=c++17 -O2 $MARCH -fno-strict-aliasing"
CFLAGS="-std=gnu11 -O2 $MARCH -D_GNU_SOURCE"

echo "== Compiling mmz_shim, port_shim, k230_init =="
$CC -std=gnu11 -O2 -march=rv64gc_xtheadcmo -D_GNU_SOURCE -I$RTINC -I$GSL -I$PORT -c $ROOT/llm/build/mmz_shim.c -o $OUT/mmz_shim.o
$CC -std=gnu11 -O2 -march=rv64gcv -D_GNU_SOURCE -I$RTINC -I$GSL -I$PORT -c $PORT/port_shim.c -o $OUT/port_shim.o
$CXX $CXXFLAGS -I$PORT -c $PORT/k230_init.cc -o $OUT/k230_init.o

echo "== Compiling kpu_vits_runner.cc =="
$CXX $CXXFLAGS \
  -I$PORT \
  -I$RTINC -I$GSL \
  -I$ORTINC \
  -I$SHERPA_SRC \
  -I$SHERPA_BUILD/_deps/kaldifst-src \
  -I$SHERPA_BUILD/_deps/openfst-src/src/include \
  -c $PORT/kpu_vits_runner.cc -o $OUT/kpu_vits_runner.o

echo "== Linking kpu_vits_runner =="
$CXX $CXXFLAGS \
  -o $OUT/kpu_vits_runner \
  $OUT/kpu_vits_runner.o $OUT/k230_init.o $OUT/mmz_shim.o $OUT/port_shim.o \
  -L$SHERPA_BUILD/install/lib \
  -L$SHERPA_BUILD/lib \
  -lsherpa-onnx-core -lsherpa-onnx-kaldifst-core -lsherpa-onnx-fst -lsherpa-onnx-fstfar -lonnxruntime \
  -L$RT/lib \
  -Wl,--start-group \
  -Wl,--whole-archive $RT/lib/libnncase.rt_modules.k230.a -Wl,--no-whole-archive \
  $RT/lib/libNncase.Runtime.Native.a \
  $RT/lib/libfunctional_k230.a \
  -Wl,--end-group \
  -Wl,--wrap=gnne_enable -Wl,--wrap=gnne_ctrl_set -Wl,--wrap=gnne_disable \
  -Wl,-rpath,/mnt/data/sherpa2/lib \
  -lpthread -ldl -lm

echo "== Build finished: $OUT/kpu_vits_runner =="
ls -lh $OUT/kpu_vits_runner
