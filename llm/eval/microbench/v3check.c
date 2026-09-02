// v3check.c - verify v3 kernel against the generic reference on random data,
// and measure both kernels' streaming speed.
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <dlfcn.h>
#include <time.h>
#include <math.h>

typedef void (*dot_fn)(int n, float *s, size_t bs, const void *vx, size_t bx,
                       const void *vy, size_t by, int nrc);

static double now_us(void) {
    struct timespec ts; clock_gettime(CLOCK_MONOTONIC, &ts);
    return ts.tv_sec * 1e6 + ts.tv_nsec / 1e3;
}

int main(int argc, char **argv) {
    const int n = argc > 1 ? atoi(argv[1]) : 1024;
    void *h = dlopen("./libggml-cpu.so.0.22.0", RTLD_NOW);
    if (!h) { fprintf(stderr, "dlopen: %s\n", dlerror()); return 1; }
    dot_fn k_ref  = (dot_fn)dlsym(h, "ggml_vec_dot_q4_K_q8_K_generic");
    dot_fn k_disp = (dot_fn)dlsym(h, "ggml_vec_dot_q4_K_q8_K");
    dot_fn k_v3   = (dot_fn)dlsym(h, "ggml_vec_dot_q4_K_q8_K_vl128_v3");

    srand(42);
    int nb = n / 256;
    void *x = aligned_alloc(64, nb * 144 + 64);
    void *y = aligned_alloc(64, nb * (256 + 36) + 64);
    uint8_t *xb = x; uint8_t *yb = y;
    for (int i = 0; i < nb; i++) {
        uint16_t d = (uint16_t)(rand() & 0x3ff), dmin = 0;
        memcpy(xb + i * 144, &d, 2); memcpy(xb + i * 144 + 2, &dmin, 2);
        for (int k = 0; k < 12; k++) xb[i * 144 + 4 + k] = (uint8_t)(1 + rand() % 127);
        for (int k = 0; k < 128; k++) xb[i * 144 + 16 + k] = (uint8_t)rand();
        float yd = 1.0f;
        memcpy(yb + i * 292, &yd, 2);
        for (int k = 0; k < 256; k++) yb[i * 292 + 8 + k] = (uint8_t)(rand() & 0x7f);
        for (int k = 0; k < 16; k++) { int16_t bs = 0; for (int l = 0; l < 16; l++) bs += (int8_t)yb[i*292+8+k*16+l]; memcpy(yb + i*292 + 264 + k*2, &bs, 2); }
    }

    if (!k_ref || !k_disp || !k_v3) { fprintf(stderr, "dlsym failed\n"); return 1; }
    float s_ref, s_v3, s_old;
    k_ref(n, &s_ref, 0, x, 0, y, 0, 1);
    setenv("GGML_RVV_Q4K", "v3", 1);    k_v3(n, &s_v3, 0, x, 0, y, 0, 1);
    setenv("GGML_RVV_Q4K", "vl128", 1); k_disp(n, &s_old, 0, x, 0, y, 0, 1);
    printf("n=%d: ref=%.3f v3=%.3f old=%.3f\n", n, s_ref, s_v3, s_old);
    printf("v3 vs ref diff=%.6f  %s\n", s_v3 - s_ref,
           fabsf(s_v3 - s_ref) < 0.05f * fabsf(s_ref) + 0.01f ? "OK" : "MISMATCH!");

    // streaming speed: 2048 rows cold (each row same content, different memory)
    const int rows = 16384;
    void *xw = aligned_alloc(64, rows * nb * 144 + 4096);
    for (int r = 0; r < rows; r++) memcpy((char*)xw + (size_t)r * nb * 144, x, nb * 144);
    float srow[1];
    setenv("GGML_RVV_Q4K", "v3", 1);
    double t0 = now_us();
    for (int r = 0; r < rows; r++)
        k_v3(n, srow, 0, (char*)xw + (size_t)r * nb * 144, nb * 144, y, 0, 1);
    double dtv3 = now_us() - t0;
    setenv("GGML_RVV_Q4K", "vl128", 1);
    t0 = now_us();
    for (int r = 0; r < rows; r++)
        k_disp(n, srow, 0, (char*)xw + (size_t)r * nb * 144, nb * 144, y, 0, 1);
    double dtold = now_us() - t0;
    double gbv3 = (double)rows * nb * 144 / (dtv3 / 1e6) / 1e9;
    double gbold = (double)rows * nb * 144 / (dtold / 1e6) / 1e9;
    printf("v3: %.2f us/row (%.2f GB/s)   old: %.2f us/row (%.2f GB/s)\n",
           dtv3 / rows, gbv3, dtold / rows, gbold);
    return 0;
}
