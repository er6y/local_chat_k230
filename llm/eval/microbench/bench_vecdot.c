// bench_vecdot.c - isolate q4_K vec_dot kernel throughput & per-call overhead on C908
// links against libggml-cpu.a (bootlin riscv64 build) - uses real dispatcher path
// v2: compares dispatcher (vl128) vs hand-scheduled vl128_v2 vs generic for correctness+speed
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <math.h>
#include "ggml.h"
#include "ggml-cpu.h"

typedef void (*vec_dot_fn)(int n, float * s, size_t bs, const void * vx, size_t bx, const void * vy, size_t by, int nrc);

// hand-written v2 kernel exported from ggml-cpu/arch/riscv/quants.c
extern void ggml_vec_dot_q4_K_q8_K_vl128_v2(int n, float * GGML_RESTRICT s, size_t bs, const void * GGML_RESTRICT vx, size_t bx, const void * GGML_RESTRICT vy, size_t by, int nrc);
// scalar reference
extern void ggml_vec_dot_q4_K_q8_K_generic(int n, float * GGML_RESTRICT s, size_t bs, const void * GGML_RESTRICT vx, size_t bx, const void * GGML_RESTRICT vy, size_t by, int nrc);

static inline uint64_t now_ns(void) {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (uint64_t)ts.tv_sec * 1000000000ull + (uint64_t)ts.tv_nsec;
}

static void bench_kernel(const char * name, vec_dot_fn fn, int n, const void * x, const void * y, int reps, float * out) {
    float result = 0.0f;
    uint64_t t0 = now_ns();
    for (int i = 0; i < reps; i++)
        fn(n, &result, 0, x, 0, y, 0, 1);
    uint64_t t1 = now_ns();
    double ns_call = (double)(t1 - t0) / reps;
    printf("  %-10s n=%4d : %7.0f ns/call = %5.2f GMAC/s = %5.0f ns/256w (result=%.3f)\n",
           name, n, ns_call, (double)n / ns_call, ns_call * 256.0 / n, result);
    *out = result;
}

int main(void) {
    setvbuf(stdout, NULL, _IONBF, 0);
    ggml_cpu_init();

    const struct ggml_type_traits_cpu * tq4k = ggml_get_type_traits_cpu(GGML_TYPE_Q4_K);
    const struct ggml_type_traits_cpu * tq8k = ggml_get_type_traits_cpu(GGML_TYPE_Q8_K);
    if (!tq4k->vec_dot || tq4k->vec_dot_type != GGML_TYPE_Q8_K) {
        printf("FATAL: no q4_K vecdot trait\n");
        return 1;
    }
    vec_dot_fn vd = tq4k->vec_dot;   // dispatcher -> vl128 on C908
    printf("q4_K vecdot: %p, nrows=%lld\n", (void*)vd, (long long)tq4k->nrows);

    // ---- build one weight row (n floats -> q4_K) and matching q8_K activation ----
    float * xf = aligned_alloc(64, 4096 * sizeof(float));
    for (int i = 0; i < 4096; i++) xf[i] = (float)(((i * 37) % 15) - 7) * 0.031f;

    const int NROW = 65536;             // rows in slab
    const int N    = 1024;              // weights per row
    const size_t qx_row = 576;          // q4_K bytes per 1024 weights (4*144)
    uint8_t * slab = aligned_alloc(4096, (size_t)NROW * qx_row);
    uint8_t * ybuf = aligned_alloc(64, 8192);

    tq4k->from_float(xf, slab, N);                       // quantize one row (row0)
    tq4k->from_float(xf, slab + (size_t)(NROW-1) * qx_row, N);
    for (int r = 1; r < NROW - 1; r++)
        memcpy(slab + (size_t)r * qx_row, slab, qx_row);
    tq8k->from_float(xf, ybuf, N);                       // quantize activation to q8_K

    float r_ref = 0, r_vd = 0, r_v2 = 0;

    // ---- test 0: correctness, kernels on identical data ----
    {
        ggml_vec_dot_q4_K_q8_K_generic(N, &r_ref, 0, slab, 0, ybuf, 0, 1);
        vd(N, &r_vd, 0, slab, 0, ybuf, 0, 1);
        ggml_vec_dot_q4_K_q8_K_vl128_v2(N, &r_v2, 0, slab, 0, ybuf, 0, 1);
        double dv = fabs(r_vd - r_ref) / (fabs(r_ref) + 1e-9);
        double d2 = fabs(r_v2 - r_ref) / (fabs(r_ref) + 1e-9);
        printf("correctness: generic=%.6f vl128=%.6f (err %.2e)  v2=%.6f (err %.2e)\n",
               r_ref, r_vd, dv, r_v2, d2);
        // multi-row check with different data to catch prefetch-path bugs (n=256 odd cases)
        for (int k = 0; k < 8; k++) {
            int n2 = 256 * (k + 1);
            uint8_t * qx = aligned_alloc(64, (size_t)(n2 / 256) * 144 + 64);
            uint8_t * qy = aligned_alloc(64, 8192);
            float * xk = aligned_alloc(64, n2 * sizeof(float));
            unsigned seed = 1234 + k * 77;
            for (int i = 0; i < n2; i++) { seed = seed * 1664525u + 1013904223u; xk[i] = (float)((int)((seed >> 16) % 15) - 7) * 0.043f; }
            tq4k->from_float(xk, qx, n2);
            tq8k->from_float(xk, qy, n2);
            float a = 0, b = 0;
            ggml_vec_dot_q4_K_q8_K_generic(n2, &a, 0, qx, 0, qy, 0, 1);
            ggml_vec_dot_q4_K_q8_K_vl128_v2(n2, &b, 0, qx, 0, qy, 0, 1);
            double dd = fabs(b - a) / (fabs(a) + 1e-9);
            printf("  n=%4d generic=%12.4f v2=%12.4f err=%.2e %s\n", n2, a, b, dd, dd < 1e-4 ? "OK" : "MISMATCH");
            free(qx); free(qy); free(xk);
        }
    }

    // ---- test 1: hot loop, same row ----
    for (int ntry = 0; ntry < 2; ntry++) {
        const int REPS = 100000;
        bench_kernel("vl128", vd, N, slab, ybuf, REPS, &r_vd);
        bench_kernel("v2", ggml_vec_dot_q4_K_q8_K_vl128_v2, N, slab, ybuf, REPS, &r_v2);
    }

    // ---- test 2: n sweep ----
    {
        const int ns[] = {256, 512, 1024, 2048};
        for (unsigned k = 0; k < sizeof(ns)/sizeof(ns[0]); k++) {
            int n = ns[k];
            size_t qxb = (size_t)(n / 256) * 144;
            uint8_t * qx = aligned_alloc(64, qxb ? qxb : 144);
            uint8_t * qy = aligned_alloc(64, 8192);
            tq4k->from_float(xf, qx, n);
            tq8k->from_float(xf, qy, n);
            int reps = 200000 / (n / 256);
            float a, b;
            bench_kernel("vl128", vd, n, qx, qy, reps, &a);
            bench_kernel("v2", ggml_vec_dot_q4_K_q8_K_vl128_v2, n, qx, qy, reps, &b);
            free(qx); free(qy);
        }
    }

    // ---- test 3: row sweep over 36 MB slab (adds memory streaming, like real GEMV) ----
    for (int ntry = 0; ntry < 2; ntry++) {
        float result = 0.0f;
        for (int which = 0; which < 2; which++) {
            vec_dot_fn fn = which ? ggml_vec_dot_q4_K_q8_K_vl128_v2 : vd;
            uint64_t t0 = now_ns();
            for (int r = 0; r < NROW; r++)
                fn(N, &result, 0, slab + (size_t)r * qx_row, 0, ybuf, 0, 1);
            uint64_t t1 = now_ns();
            double ns_call = (double)(t1 - t0) / NROW;
            printf("  %-10s sweep %d rows(36MB): %7.0f ns/call = %5.2f GMAC/s = %5.0f MB/s\n",
                   which ? "v2" : "vl128", NROW, ns_call, (double)N / ns_call,
                   (double)NROW * qx_row / (t1 - t0) * 1e9 / 1e6);
        }
    }

    // ---- test 4: activation quantize cost ----
    {
        uint64_t t0 = now_ns();
        for (int i = 0; i < 100000; i++)
            tq8k->from_float(xf, ybuf, N);
        uint64_t t1 = now_ns();
        printf("q8_K quantize n=%d : %7.0f ns/call\n", N, (double)(t1 - t0) / 100000);
    }

    printf("VECDOT_BENCH_DONE\n");
    return 0;
}
