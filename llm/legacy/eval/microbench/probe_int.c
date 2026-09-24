#include <riscv_vector.h>
// probe: correct mask type + reinterpret direction on gcc 14.3
vuint32m2_t probe_reint(vfloat32m2_t a) {
    return __riscv_vreinterpret_v_u32m2_f32m2(a);
}
vbool16_t probe_mseq(vuint32m2_t a, size_t vl) {
    return __riscv_vmseq_vx_u32m2_b16(a, 0, vl);
}
vuint32m2_t probe_merge(vuint32m2_t a, vuint32m2_t b, vbool16_t m, size_t vl) {
    return __riscv_vmerge_vvm_u32m2(a, b, m, vl);
}
