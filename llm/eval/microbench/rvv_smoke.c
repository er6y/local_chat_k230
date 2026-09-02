// rvv_smoke.c - minimal RVV 1.0 smoke test for C908: dot product + Q7 dot Q7
// builds: run 3 rounds, prints checksum & GFLOP-ish timing
#include <riscv_vector.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

static double now_ms(void) {
    struct timespec ts; clock_gettime(CLOCK_MONOTONIC, &ts);
    return ts.tv_sec * 1000.0 + ts.tv_nsec / 1e6;
}

float dot_f32(const float *x, const float *y, int n) {
    size_t vl;
    vfloat32m8_t acc = __riscv_vfmv_v_f_f32m8(0.0f, __riscv_vsetvlmax_e32m8());
    for (size_t i = 0; i < (size_t)n; ) {
        vl = __riscv_vsetvl_e32m8(n - i);
        vfloat32m8_t vx = __riscv_vle32_v_f32m8(x + i, vl);
        vfloat32m8_t vy = __riscv_vle32_v_f32m8(y + i, vl);
        acc = __riscv_vfmacc_vv_f32m8(acc, vx, vy, vl);
        i += vl;
    }
    vfloat32m1_t s = __riscv_vfmv_v_f_f32m1(0.0f, __riscv_vsetvlmax_e32m1());
    s = __riscv_vfredusum_vs_f32m8_f32m1(acc, s, __riscv_vsetvlmax_e32m8());
    return __riscv_vfmv_f_s_f32m1_f32(s);
}

int dot_i8(const int8_t *x, const int8_t *y, int n) {
    size_t vl;
    vint16m8_t acc = __riscv_vmv_v_x_i16m8(0, __riscv_vsetvlmax_e16m8());
    for (size_t i = 0; i < (size_t)n; ) {
        vl = __riscv_vsetvl_e8m4(n - i);
        vint8m4_t vx = __riscv_vle8_v_i8m4(x + i, vl);
        vint8m4_t vy = __riscv_vle8_v_i8m4(y + i, vl);
        vint16m8_t px = __riscv_vsext_vf2_i16m8(vx, vl);
        vint16m8_t py = __riscv_vsext_vf2_i16m8(vy, vl);
        acc = __riscv_vmacc_vv_i16m8(acc, px, py, vl);
        i += vl;
    }
    vint32m1_t z = __riscv_vmv_v_x_i32m1(0, __riscv_vsetvlmax_e32m1());
    z = __riscv_vwredsum_vs_i16m8_i32m1(acc, z, __riscv_vsetvlmax_e16m8());
    return __riscv_vmv_x_s_i32m1_i32(z);
}

int main(void) {
    const int N = 4096;
    float *fx = aligned_alloc(64, N * 4), *fy = aligned_alloc(64, N * 4);
    int8_t *ix = aligned_alloc(64, N), *iy = aligned_alloc(64, N);
    for (int i = 0; i < N; i++) {
        fx[i] = (float)(i % 17) * 0.25f; fy[i] = (float)((i * 7) % 23) * 0.5f;
        ix[i] = (int8_t)(i % 19 - 9);    iy[i] = (int8_t)((i * 5) % 21 - 10);
    }
    // scalar reference
    float ref_f = 0; long ref_i = 0;
    for (int i = 0; i < N; i++) { ref_f += fx[i] * fy[i]; ref_i += (long)ix[i] * iy[i]; }

    float got_f = dot_f32(fx, fy, N);
    int got_i = dot_i8(ix, iy, N);
    printf("f32 dot: ref=%.2f rvv=%.2f %s\n", ref_f, got_f, (ref_f - got_f) * (ref_f - got_f) < 1e-3 ? "OK" : "MISMATCH");
    printf("i8  dot: ref=%ld rvv=%d %s\n", ref_i, got_i, ref_i == got_i ? "OK" : "MISMATCH");

    // throughput: 1000 iterations of f32 dot
    double t0 = now_ms();
    float sink = 0;
    for (int r = 0; r < 1000; r++) sink += dot_f32(fx, fy, N);
    double dt = now_ms() - t0;
    printf("f32 dot x1000 (%d elems): %.1f ms -> %.2f GFLOP/s (sink=%.1f)\n", N, dt, 2.0 * N * 1000 / (dt / 1000.0) / 1e9, sink);

    t0 = now_ms();
    long sink2 = 0;
    for (int r = 0; r < 1000; r++) sink2 += dot_i8(ix, iy, N);
    dt = now_ms() - t0;
    printf("i8  dot x1000: %.1f ms -> %.2f GOPS (sink=%ld)\n", dt, 2.0 * N * 1000 / (dt / 1000.0) / 1e9, sink2);
    printf("VLEN=%zu bytes\n", (size_t)__riscv_vlenb());
    printf("RVV SMOKE TEST DONE\n");
    return 0;
}
