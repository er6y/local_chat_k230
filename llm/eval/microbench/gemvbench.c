// gemvbench.c - simulate the real GEMV: 2048 rows x 576B, cold streaming,
// one kernel call per row (current) vs 4 rows per call (target design).
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <dlfcn.h>
#include <time.h>

typedef void (*dot_fn)(int n, float *s, size_t bs, const void *vx, size_t bx,
                       const void *vy, size_t by, int nrc);

static double now_us(void) {
    struct timespec ts; clock_gettime(CLOCK_MONOTONIC, &ts);
    return ts.tv_sec * 1e6 + ts.tv_nsec / 1e3;
}

int main(void) {
    void *h = dlopen("./libggml-cpu.so.0.22.0", RTLD_NOW);
    dot_fn fn = (dot_fn)dlsym(h, "ggml_vec_dot_q4_K_q8_K");
    const int n = 1024, rows = 2048;
    const size_t B4 = 144, B8 = 256 + 16 + 16;
    void *x = aligned_alloc(64, rows * (n/256) * B4 + 4096);   // 1.18MB weights
    void *y = aligned_alloc(64, (n/256) * B8 + 64);            // activation
    memset(x, 0x11, rows * (n/256) * B4);
    memset(y, 0x01, (n/256) * B8);
    float s[64] = {0};
    size_t bx = (n/256) * B4;

    // current: one call per row
    double t0 = now_us();
    for (int r = 0; r < rows; r++)
        fn(n, &s[r & 63], 0, (char *)x + (size_t)r * bx, bx, y, 0, 1);
    double dt1 = now_us() - t0;
    printf("current (1 row/call): %.2f us/row, %d rows in %.1f ms\n",
           dt1 / rows, rows, dt1 / 1000);

    // target: 4 rows per call, same total
    t0 = now_us();
    for (int r = 0; r < rows; r += 4)
        for (int c = 0; c < 4; c++)
            fn(n, &s[(r + c) & 63], 0, (char *)x + (size_t)(r + c) * bx, bx, y, 0, 1);
    double dt2 = now_us() - t0;
    printf("4x unrolled calls:     %.2f us/row (%.1f ms total)\n",
           dt2 / rows, dt2 / 1000);

    // raw memcpy bandwidth reference: read the whole 1.18MB 100 times
    void *dst = aligned_alloc(64, 1 << 20);
    t0 = now_us();
    for (int i = 0; i < 200; i++)
        memcpy(dst, x, rows * bx);
    double dt3 = now_us() - t0;
    double gbs = (double)rows * bx * 200 / (dt3 / 1e6) / 1e9;
    printf("memcpy stream: %.2f GB/s\n", gbs);
    return 0;
}
