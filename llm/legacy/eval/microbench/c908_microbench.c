// c908_microbench.c - C908 vector/scalar/memory throughput microbenchmark
// build: riscv64 gcc -march=rv64gcv_zicbop_zihintpause -static -O2
// measures cycles per iteration for 8 unrolled independent ops
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

static inline uint64_t now_ns(void) {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (uint64_t)ts.tv_sec * 1000000000ull + (uint64_t)ts.tv_nsec;
}

#define NITER 200000

static double bench(const char *name, void (*fn)(void)) {
    uint64_t t0 = now_ns();
    fn();
    uint64_t t1 = now_ns();
    double npi = (double)(t1 - t0) / NITER;
    printf("%-22s %8.1f ns/iter  (%.2f ns/op)\n", name, npi, npi / 8.0);
    return npi;
}

// data pools
static int8_t  d8[256] __attribute__((aligned(64)));
static int16_t d16[256] __attribute__((aligned(64)));
static float   d32[256] __attribute__((aligned(64)));
static int32_t d32i[256] __attribute__((aligned(64)));

// ---------- f32 vfmacc.vv, VL=4, 8 independent accumulators ----------
static void b_fmac_vv(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 4, e32, m1, ta, ma\n\t"
            "vfmacc.vv v8,  v1, v2\n\t"
            "vfmacc.vv v9,  v1, v2\n\t"
            "vfmacc.vv v10, v1, v2\n\t"
            "vfmacc.vv v11, v1, v2\n\t"
            "vfmacc.vv v12, v1, v2\n\t"
            "vfmacc.vv v13, v1, v2\n\t"
            "vfmacc.vv v14, v1, v2\n\t"
            "vfmacc.vv v15, v1, v2\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- f32 vfmacc.vf (scalar broadcast), VL=4 ----------
static void b_fmac_vf(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 4, e32, m1, ta, ma\n\t"
            "vfmacc.vf v8,  fa0, v2\n\t"
            "vfmacc.vf v9,  fa0, v2\n\t"
            "vfmacc.vf v10, fa0, v2\n\t"
            "vfmacc.vf v11, fa0, v2\n\t"
            "vfmacc.vf v12, fa0, v2\n\t"
            "vfmacc.vf v13, fa0, v2\n\t"
            "vfmacc.vf v14, fa0, v2\n\t"
            "vfmacc.vf v15, fa0, v2\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- f32 vfmacc m2 (VL=8) ----------
static void b_fmac_m2(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 8, e32, m2, ta, ma\n\t"
            "vfmacc.vv v8,  v2, v4\n\t"
            "vfmacc.vv v10, v2, v4\n\t"
            "vfmacc.vv v12, v2, v4\n\t"
            "vfmacc.vv v14, v2, v4\n\t"
            "vfmacc.vv v16, v2, v4\n\t"
            "vfmacc.vv v18, v2, v4\n\t"
            "vfmacc.vv v20, v2, v4\n\t"
            "vfmacc.vv v22, v2, v4\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- i8 vwmacc.vv (widen i8*i8->i16), VL=16, m2 accumulators ----------
static void b_vwmac_i8(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 16, e8, m1, ta, ma\n\t"
            "vwmacc.vv v8,  v1, v2\n\t"
            "vwmacc.vv v10, v1, v3\n\t"
            "vwmacc.vv v12, v1, v4\n\t"
            "vwmacc.vv v14, v1, v5\n\t"
            "vwmacc.vv v16, v1, v6\n\t"
            "vwmacc.vv v18, v1, v7\n\t"
            "vwmacc.vv v20, v1, v24\n\t"
            "vwmacc.vv v22, v1, v25\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- i16 vmacc.vv VL=8 ----------
static void b_vmac_i16(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 8, e16, m1, ta, ma\n\t"
            "vmacc.vv v8,  v1, v2\n\t"
            "vmacc.vv v9,  v1, v2\n\t"
            "vmacc.vv v10, v1, v2\n\t"
            "vmacc.vv v11, v1, v2\n\t"
            "vmacc.vv v12, v1, v2\n\t"
            "vmacc.vv v13, v1, v2\n\t"
            "vmacc.vv v14, v1, v2\n\t"
            "vmacc.vv v15, v1, v2\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- i8 vmacc.vv VL=16 (same-width, overflow ignored) ----------
static void b_vmac_i8(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 16, e8, m1, ta, ma\n\t"
            "vmacc.vv v8,  v1, v2\n\t"
            "vmacc.vv v9,  v1, v2\n\t"
            "vmacc.vv v10, v1, v2\n\t"
            "vmacc.vv v11, v1, v2\n\t"
            "vmacc.vv v12, v1, v2\n\t"
            "vmacc.vv v13, v1, v2\n\t"
            "vmacc.vv v14, v1, v2\n\t"
            "vmacc.vv v15, v1, v2\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- vwredsum.vs i16m2 -> i32m1, 8 independent reductions ----------
static void b_vwredsum(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 16, e16, m2, ta, ma\n\t"
            "vmv.v.x v0, zero\n\t"
            "vwredsum.vs v4, v8,  v0\n\t"
            "vwredsum.vs v5, v10, v0\n\t"
            "vwredsum.vs v6, v12, v0\n\t"
            "vwredsum.vs v7, v14, v0\n\t"
            "vwredsum.vs v25, v16, v0\n\t"
            "vwredsum.vs v26, v18, v0\n\t"
            "vwredsum.vs v27, v20, v0\n\t"
            "vwredsum.vs v28, v22, v0\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- vredsum.vs i32 m1, 8 independent ----------
static void b_vredsum(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 4, e32, m1, ta, ma\n\t"
            "vmv.v.x v0, zero\n\t"
            "vredsum.vs v4, v8,  v0\n\t"
            "vredsum.vs v5, v9,  v0\n\t"
            "vredsum.vs v6, v10, v0\n\t"
            "vredsum.vs v7, v11, v0\n\t"
            "vredsum.vs v25, v12, v0\n\t"
            "vredsum.vs v26, v13, v0\n\t"
            "vredsum.vs v27, v14, v0\n\t"
            "vredsum.vs v28, v15, v0\n\t"
            ::: "memory", "v0", "v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16", "v17", "v18", "v19", "v20", "v21", "v22", "v23", "v24", "v25", "v26", "v27", "v28", "v29", "v30", "v31", "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31");
    }
}

// ---------- vle8.v 8 independent loads (streaming from L1-resident buffer) ----------
static void b_vle8(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 16, e8, m1, ta, ma\n\t"
            "vle8.v v8,  (%0)\n\t"
            "vle8.v v9,  (%1)\n\t"
            "vle8.v v10, (%2)\n\t"
            "vle8.v v11, (%3)\n\t"
            "vle8.v v12, (%4)\n\t"
            "vle8.v v13, (%5)\n\t"
            "vle8.v v14, (%6)\n\t"
            "vle8.v v15, (%7)\n\t"
            :: "r"(d8), "r"(d8 + 16), "r"(d8 + 32), "r"(d8 + 48),
               "r"(d8 + 64), "r"(d8 + 80), "r"(d8 + 96), "r"(d8 + 112)
            : "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15");
    }
}

// ---------- vle32.v 8 loads ----------
static void b_vle32(void) {
    for (int i = 0; i < NITER; i++) {
        asm volatile(
            "vsetivli zero, 4, e32, m1, ta, ma\n\t"
            "vle32.v v8,  (%0)\n\t"
            "vle32.v v9,  (%1)\n\t"
            "vle32.v v10, (%2)\n\t"
            "vle32.v v11, (%3)\n\t"
            "vle32.v v12, (%4)\n\t"
            "vle32.v v13, (%5)\n\t"
            "vle32.v v14, (%6)\n\t"
            "vle32.v v15, (%7)\n\t"
            :: "r"(d32), "r"(d32 + 4), "r"(d32 + 8), "r"(d32 + 12),
               "r"(d32 + 16), "r"(d32 + 20), "r"(d32 + 24), "r"(d32 + 28)
            : "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15");
    }
}

// ---------- scalar imul+add chain, 8 independent chains ----------
static void b_scalar_mul(void) {
    volatile int sink = 0;
    int a = 3, b = 5;
    int r0 = 0, r1 = 0, r2 = 0, r3 = 0, r4 = 0, r5 = 0, r6 = 0, r7 = 0;
    for (int i = 0; i < NITER; i++) {
        r0 += a * b; r1 += a * b; r2 += a * b; r3 += a * b;
        r4 += a * b; r5 += a * b; r6 += a * b; r7 += a * b;
    }
    sink = r0 + r1 + r2 + r3 + r4 + r5 + r6 + r7;
    (void)sink;
}

// ---------- scalar byte: load + nibble unpack + mul + add (q4-style) ----------
static void b_scalar_q4(void) {
    volatile int sink = 0;
    const uint8_t *p = (const uint8_t *)d8;
    const int8_t *q = (const int8_t *)d16;
    int s0 = 0, s1 = 0, s2 = 0, s3 = 0;
    for (int i = 0; i < NITER; i++) {
        uint8_t b0 = p[i & 255];
        int lo = (b0 & 0xF) - 8;
        int hi = (b0 >> 4) - 8;
        s0 += lo * q[i & 255];
        s1 += hi * q[(i + 1) & 255];
        s2 += lo * q[(i + 2) & 255];
        s3 += hi * q[(i + 3) & 255];
    }
    sink = s0 + s1 + s2 + s3;
    (void)sink;
}

// ---------- memory bandwidth: 8MB copy ----------
static char src_buf[1 << 20] __attribute__((aligned(64)));
static char dst_buf[1 << 20] __attribute__((aligned(64)));
static void b_memcpy(void) {
    uint64_t t0 = now_ns();
    for (int i = 0; i < 8; i++)
        memcpy(dst_buf, src_buf, sizeof(src_buf));
    uint64_t t1 = now_ns();
    double bytes = 8.0 * 2.0 * sizeof(src_buf); // read + write
    printf("%-22s %8.0f ns  (%.0f MB/s)\n", "memcpy 8MB r+w",
           (double)(t1 - t0), bytes / ((t1 - t0) / 1e9) / 1e6);
}

int main(void) {
    setvbuf(stdout, NULL, _IONBF, 0);
    // init data
    for (int i = 0; i < 256; i++) { d8[i] = (int8_t)i; d16[i] = (int16_t)(i * 3); d32[i] = i * 0.5f; d32i[i] = i; }
    memset(src_buf, 0xAB, sizeof(src_buf));

    printf("=== C908 microbench (8 ops/iter, 1GHz assumed) ===\n");
    bench("f32 vfmacc.vv VL4", b_fmac_vv);
    bench("f32 vfmacc.vf VL4", b_fmac_vf);
    bench("f32 vfmacc.vv VL8m2", b_fmac_m2);
    bench("i8 vwmacc.vv VL16", b_vwmac_i8);
    bench("i16 vmacc.vv VL8", b_vmac_i16);
    bench("i8 vmacc.vv VL16", b_vmac_i8);
    bench("vwredsum i16->i32", b_vwredsum);
    bench("vredsum i32", b_vredsum);
    bench("vle8.v x8", b_vle8);
    bench("vle32.v x8", b_vle32);
    bench("scalar mul+add x8", b_scalar_mul);
    bench("scalar q4 dot x4", b_scalar_q4);
    b_memcpy();
    printf("MICROBENCH_DONE\n");
    return 0;
}
