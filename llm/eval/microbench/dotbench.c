// dotbench.c - isolate ggml_vec_dot_q4_K_q8_K cost on the board
// dlopen the deployed .so, dlsym the kernel, time 100k calls at n=1024/2048.
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

int main(int argc, char **argv) {
    const int n = argc > 1 ? atoi(argv[1]) : 1024;   // QK_K multiple
    const char *sym = argc > 2 ? argv[2] : "ggml_vec_dot_q4_K_q8_K";
    void *h = dlopen("./libggml-cpu.so.0.22.0", RTLD_NOW);
    if (!h) { fprintf(stderr, "dlopen: %s\n", dlerror()); return 1; }
    dot_fn fn = (dot_fn)dlsym(h, sym);
    if (!fn) { fprintf(stderr, "dlsym %s: %s\n", sym, dlerror()); return 1; }

    // block_q4_K layout: 256 nibbles + 2x6 scales + 8 mins + 12 bytes...
    // block_q4_K size = 144 bytes; block_q8_K size = 256+16+16 = 288? use exact:
    const size_t B4 = 144, B8 = 256 + 16 + 16;
    int nb = n / 256;
    void *x = aligned_alloc(64, nb * B4 + 64);
    void *y = aligned_alloc(64, nb * B8 + 64);
    memset(x, 0x11, nb * B4);
    memset(y, 0x01, nb * B8);
    float s = 0;

    for (int i = 0; i < 1000; i++) fn(n, &s, 0, x, 0, y, 0, 1);   // warm
    const int iters = 200000;
    double t0 = now_us();
    for (int i = 0; i < iters; i++) fn(n, &s, 0, x, 0, y, 0, 1);
    double dt = now_us() - t0;
    printf("%s n=%d: %.2f us/call  (%.2f GOPS at 2*%d*%d MACs)\n",
           sym, n, dt / iters, 2.0 * n * iters / (dt / 1e6) / 1e9, n, 1);
    printf("result s=%f\n", s);
    return 0;
}
