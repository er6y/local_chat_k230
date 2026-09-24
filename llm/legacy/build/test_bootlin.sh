#!/bin/bash
# test_bootlin.sh - verify bootlin toolchain + RVV 1.0 intrinsics
TC=/root/toolchains/riscv64-lp64d--glibc--stable-2025.08-1
ls $TC/bin | grep -E 'riscv64.*-(gcc|objdump)$' | head -6
GCC=$TC/bin/riscv64-buildroot-linux-gnu-gcc
if [ ! -x $GCC ]; then GCC=$(ls $TC/bin/*-gcc | grep -v x86 | head -1); fi
echo "GCC=$GCC"
$GCC --version | head -1
echo "=== rvv intrinsic test ==="
cat > /tmp/t2.c <<'EOF'
#include <riscv_vector.h>
float dot(const float*x, int n){
    size_t vl = __riscv_vsetvl_e32m8(n);
    vfloat32m8_t v = __riscv_vle32_v_f32m8(x, vl);
    vfloat32m1_t s = __riscv_vfmv_v_f_f32m1(0, vl);
    return __riscv_vfmv_f_s_f32m1_f32(__riscv_vfredusum_vs_f32m8_f32m1(v, s, vl));
}
EOF
$GCC -march=rv64gcv -mabi=lp64d -O2 -c /tmp/t2.c -o /tmp/t2.o && echo COMPILE_OK
OBJDUMP=$(echo $GCC | sed 's/-gcc$/-objdump/')
$OBJDUMP -d /tmp/t2.o | grep -E 'vsetvl|vle32|vfred' | head -5
echo "vector-insn-count: $($OBJDUMP -d /tmp/t2.o | grep -cE 'vsetvl|vle[0-9]|vfred')"
echo "=== hello world link test (glibc sysroot) ==="
echo 'int main(){return 0;}' > /tmp/h.c
$GCC -march=rv64gcv -mabi=lp64d -o /tmp/h /tmp/h.c && echo LINK_OK && file /tmp/h
