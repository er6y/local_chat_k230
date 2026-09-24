// ddr_bw.c - measure effective DDR read bandwidth (streaming, 32MB buffer)
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <time.h>

static inline uint64_t now_ns(void) {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (uint64_t)ts.tv_sec * 1000000000ull + (uint64_t)ts.tv_nsec;
}

#define BUF_BYTES (32u << 20)
static uint8_t *buf;

// scalar streaming read, 64B stride (cache line), accumulate XOR to defeat prefetch elimination
static uint64_t sum_read_stride(void) {
    uint64_t acc = 0;
    uint64_t n = BUF_BYTES;
    uint64_t t0 = now_ns();
    for (uint64_t off = 0; off < n; off += 64)
        acc += buf[off];
    uint64_t t1 = now_ns();
    printf("read stride64 : %6.0f MB/s (acc=%llu)\n",
           (double)n / ((t1 - t0) / 1e9) / 1e6, (unsigned long long)acc);
    return t1 - t0;
}

// vector streaming read vle8 m8, 128B per op
static void sum_read_vec(void) {
    uint64_t n = BUF_BYTES;
    uint64_t t0 = now_ns();
    uint64_t chunks = n / 128;
    uint8_t *p = buf;
    asm volatile(
        "vsetivli zero, 16, e8, m8, ta, ma\n\t"
        "1:\n\t"
        "vle8.v v8, (%[p])\n\t"
        "addi %[p], %[p], 128\n\t"
        "addi %[n], %[n], -1\n\t"
        "bnez %[n], 1b\n\t"
        : [p] "+r"(p), [n] "+r"(chunks)
        :
        : "memory", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15");
    uint64_t t1 = now_ns();
    printf("read vle8 m8  : %6.0f MB/s\n", (double)n / ((t1 - t0) / 1e9) / 1e6);
}

// read+write copy 16MB
static void copy_bw(void) {
    uint64_t n = BUF_BYTES / 2;
    uint64_t t0 = now_ns();
    for (uint64_t off = 0; off < n; off += 64) {
        buf[n + off] = buf[off];
    }
    uint64_t t1 = now_ns();
    printf("copy r+w      : %6.0f MB/s total traffic\n", (double)(2 * n) / ((t1 - t0) / 1e9) / 1e6);
}

int main(void) {
    setvbuf(stdout, NULL, _IONBF, 0);
    buf = malloc(BUF_BYTES);
    for (uint64_t i = 0; i < BUF_BYTES; i += 4096) buf[i] = (uint8_t)(i ^ (i >> 9));
    printf("=== DDR bandwidth (32MB buffer, uncached streaming) ===\n");
    sum_read_stride();
    sum_read_stride();      // second run
    sum_read_vec();
    sum_read_vec();
    copy_bw();
    printf("DDR_BW_DONE\n");
    return 0;
}
