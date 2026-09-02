// m2_probe.c - isolate the hanging f32 m2 vfmacc case
#include <stdio.h>
#include <stdint.h>
#include <time.h>

static inline uint64_t now_ns(void) {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (uint64_t)ts.tv_sec * 1000000000ull + (uint64_t)ts.tv_nsec;
}

#define NIT 100000

int main(void) {
    setvbuf(stdout, NULL, _IONBF, 0);

    printf("A: f32 m1 vfmacc x8\n");
    uint64_t t0 = now_ns();
    for (int i = 0; i < NIT; i++) {
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
            ::: "memory", "v0","v1","v2","v3","v4","v5","v6","v7","v8","v9","v10","v11","v12","v13","v14","v15","v16","v17","v18","v19","v20","v21","v22","v23","v24","v25","v26","v27","v28","v29","v30","v31");
    }
    printf("   %.2f ns/iter\n", (double)(now_ns()-t0)/NIT);

    printf("B: f32 m2 vfmacc, vd v8/v10, src v1(m2)/v4(m2)\n");
    t0 = now_ns();
    for (int i = 0; i < NIT; i++) {
        asm volatile(
            "vsetivli zero, 8, e32, m2, ta, ma\n\t"
            "vfmacc.vv v8,  v1, v4\n\t"
            "vfmacc.vv v10, v1, v4\n\t"
            "vfmacc.vv v12, v1, v4\n\t"
            "vfmacc.vv v14, v1, v4\n\t"
            "vfmacc.vv v16, v1, v4\n\t"
            "vfmacc.vv v18, v1, v4\n\t"
            "vfmacc.vv v20, v1, v4\n\t"
            "vfmacc.vv v22, v1, v4\n\t"
            ::: "memory", "v0","v1","v2","v3","v4","v5","v6","v7","v8","v9","v10","v11","v12","v13","v14","v15","v16","v17","v18","v19","v20","v21","v22","v23","v24","v25","v26","v27","v28","v29","v30","v31");
    }
    printf("   %.2f ns/iter\n", (double)(now_ns()-t0)/NIT);

    printf("C: f32 m2 vfmacc, src v1/v2 (vl-reg)\n");
    t0 = now_ns();
    for (int i = 0; i < NIT; i++) {
        asm volatile(
            "vsetivli zero, 8, e32, m2, ta, ma\n\t"
            "vfmacc.vv v8,  v1, v2\n\t"
            "vfmacc.vv v10, v1, v2\n\t"
            "vfmacc.vv v12, v1, v2\n\t"
            "vfmacc.vv v14, v1, v2\n\t"
            "vfmacc.vv v16, v1, v2\n\t"
            "vfmacc.vv v18, v1, v2\n\t"
            "vfmacc.vv v20, v1, v2\n\t"
            "vfmacc.vv v22, v1, v2\n\t"
            ::: "memory", "v0","v1","v2","v3","v4","v5","v6","v7","v8","v9","v10","v11","v12","v13","v14","v15","v16","v17","v18","v19","v20","v21","v22","v23","v24","v25","v26","v27","v28","v29","v30","v31");
    }
    printf("   %.2f ns/iter\n", (double)(now_ns()-t0)/NIT);

    printf("M2_PROBE_DONE\n");
    return 0;
}
