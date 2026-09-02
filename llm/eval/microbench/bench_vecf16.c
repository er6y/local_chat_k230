// bench_vecf16.c - validate & bench the no-zvfh RVV path of ggml_vec_dot_f16
// and the RVV elementwise branches (add/acc/sub/mul f32) added in vec.cpp/vec.h.
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <math.h>
#include <ggml.h>
#include "vec.h"   // static-inline elementwise (add/acc/sub/mul) - same path the library uses

// vec_dot_f16 is defined in vec.cpp (non-static), linked from libggml-cpu.a
extern void ggml_vec_dot_f16(int n, float * s, size_t bs, uint16_t * x, size_t bx, uint16_t * y, size_t by, int nrc);

static inline uint64_t now_ns(void) {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (uint64_t)ts.tv_sec * 1000000000ull + (uint64_t)ts.tv_nsec;
}

// scalar f16->f32 (IEEE 754 binary16)
static inline float h2f(uint16_t h) {
    uint32_t sign = (uint32_t)(h & 0x8000) << 16;
    uint32_t e = (h >> 10) & 0x1F;
    uint32_t m = h & 0x3FF;
    if (e == 0) {
        if (m == 0) { float f; memcpy(&f, &sign, 4); return f; }
        // subnormal: m * 2^-24
        float val = (float)m * (1.0f / 16777216.0f);
        float out = sign ? -val : val;
        return out;
    }
    if (e == 31) {
        uint32_t u = sign | 0x7F800000u | (m << 13);
        float f; memcpy(&f, &u, 4); return f;
    }
    uint32_t u = sign | ((e - 15 + 127) << 23) | (m << 13);
    float f; memcpy(&f, &u, 4);
    return f;
}

int main(void) {
    int fail = 0;

    // ---------- vec_dot_f16 ----------
    static const int ns[] = {1024, 4096};
    for (int k = 0; k < 2; k++) {
        int n = ns[k];
        uint16_t * x = aligned_alloc(64, n * 2);
        uint16_t * y = aligned_alloc(64, n * 2);
        unsigned seed = 42 + k;
        float ref = 0;
        for (int i = 0; i < n; i++) {
            seed = seed * 1664525u + 1013904223u;
            float a = (float)((int)((seed >> 16) % 13) - 6) * 0.13f;
            seed = seed * 1664525u + 1013904223u;
            float b = (float)((int)((seed >> 16) % 13) - 6) * 0.11f;
            x[i] = ggml_fp32_to_fp16(a);
            y[i] = ggml_fp32_to_fp16(b);
            ref += h2f(x[i]) * h2f(y[i]);
        }
        float r = 0;
        ggml_vec_dot_f16(n, &r, 0, x, 0, y, 0, 1);
        double err = fabs(r - ref) / (fabs(ref) + 1e-9);
        printf("vec_dot_f16 n=%5d: ref=%.5f rvv=%.5f err=%.2e %s\n", n, ref, r, err, err < 1e-4 ? "OK" : "MISMATCH");
        if (err >= 1e-4) fail++;
        free(x); free(y);
    }

    // special values: zeros, subnormals, inf/nan excluded (mapped to large finite by design)
    {
        int n = 64;
        uint16_t x[64], y[64];
        float ref = 0;
        for (int i = 0; i < n; i++) { x[i] = 0; y[i] = ggml_fp32_to_fp16(0.5f); }
        for (int i = 0; i < 32; i++) x[i] = ggml_fp32_to_fp16(1.5f);   // first half normal, rest zero
        for (int i = 0; i < n; i++) ref += h2f(x[i]) * h2f(y[i]);
        float r = 0;
        ggml_vec_dot_f16(n, &r, 0, x, 0, y, 0, 1);
        double err = fabs(r - ref) / (fabs(ref) + 1e-9);
        printf("vec_dot_f16 zeros  : ref=%.5f rvv=%.5f err=%.2e %s\n", ref, r, err, err < 1e-4 ? "OK" : "MISMATCH");
        if (err >= 1e-4) fail++;
    }

    // ---------- elementwise f32 ----------
    {
        int n = 4096;
        float * x = aligned_alloc(64, n * 4);
        float * y = aligned_alloc(64, n * 4);
        float * z = aligned_alloc(64, n * 4);
        float * zr = aligned_alloc(64, n * 4);
        for (int i = 0; i < n; i++) { x[i] = (float)(i % 17) * 0.07f - 0.6f; y[i] = (float)((i * 13) % 23) * 0.03f - 0.3f; }

        double maxerr = 0;
        ggml_vec_add_f32(n, z, x, y);
        for (int i = 0; i < n; i++) { zr[i] = x[i] + y[i]; double e = fabs(z[i] - zr[i]); if (e > maxerr) maxerr = e; }
        printf("vec_add_f32 : maxerr=%.2e %s\n", maxerr, maxerr < 1e-6 ? "OK" : "MISMATCH"); if (maxerr >= 1e-6) fail++;

        maxerr = 0;
        memcpy(z, y, n * 4);
        ggml_vec_acc_f32(n, z, x);
        for (int i = 0; i < n; i++) { zr[i] = y[i] + x[i]; double e = fabs(z[i] - zr[i]); if (e > maxerr) maxerr = e; }
        printf("vec_acc_f32 : maxerr=%.2e %s\n", maxerr, maxerr < 1e-6 ? "OK" : "MISMATCH"); if (maxerr >= 1e-6) fail++;

        maxerr = 0;
        ggml_vec_sub_f32(n, z, x, y);
        for (int i = 0; i < n; i++) { zr[i] = x[i] - y[i]; double e = fabs(z[i] - zr[i]); if (e > maxerr) maxerr = e; }
        printf("vec_sub_f32 : maxerr=%.2e %s\n", maxerr, maxerr < 1e-6 ? "OK" : "MISMATCH"); if (maxerr >= 1e-6) fail++;

        maxerr = 0;
        ggml_vec_mul_f32(n, z, x, y);
        for (int i = 0; i < n; i++) { zr[i] = x[i] * y[i]; double e = fabs(z[i] - zr[i]); if (e > maxerr) maxerr = e; }
        printf("vec_mul_f32 : maxerr=%.2e %s\n", maxerr, maxerr < 1e-6 ? "OK" : "MISMATCH"); if (maxerr >= 1e-6) fail++;

        // hot timing (ns/elem)
        const int R = 20000;
        uint64_t t0, t1;
        t0 = now_ns(); for (int r = 0; r < R; r++) ggml_vec_dot_f16(n, &z[0], 0, (uint16_t*)x, 0, (uint16_t*)y, 0, 1); t1 = now_ns();
        printf("timing vec_dot_f16 n=4096: %.0f ns/call (reinterpreted bits, perf only)\n", (double)(t1 - t0) / R);
        t0 = now_ns(); for (int r = 0; r < R; r++) ggml_vec_mul_f32(n, z, x, y); t1 = now_ns();
        printf("timing vec_mul_f32 n=4096: %.0f ns/call = %.2f elem/cycle@1.6G\n", (double)(t1 - t0) / R, 4096.0 / ((t1 - t0) / R * 1.6e-3 + 1e-9));
        t0 = now_ns(); for (int r = 0; r < R; r++) ggml_vec_add_f32(n, z, x, y); t1 = now_ns();
        printf("timing vec_add_f32 n=4096: %.0f ns/call\n", (double)(t1 - t0) / R);
        free(x); free(y); free(z); free(zr);
    }

    printf(fail ? "VEC_BENCH FAIL (%d)\n" : "VEC_BENCH PASS\n", fail);
    return fail ? 1 : 0;
}
