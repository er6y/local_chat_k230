// p1_dump.c - isolate the scales-decode phase: run ORIGINAL vl128 P1 asm vs v2 P1 asm
// on an identical synthetic block head and dump utmp[0..3] for comparison.
#include <stdio.h>
#include <stdint.h>

static uint32_t head0[4] __attribute__((aligned(16)));

__attribute__((noinline))
static void p1_orig(const void * blk, uint32_t * utmp) {
    static const uint32_t kmask1 = 0x3f3f3f3f;
    static const uint32_t kmask2 = 0x0f0f0f0f;
    static const uint32_t kmask3 = 0x03030303;
    long s0, s1;
    __asm__ __volatile__(
        "li %[s1], 8\n\t"
        "vsetivli zero, 4, e32, m1, ta, ma\n\t"
        "vle32.v v1, (%[s6b])\n\t"
        "vslide1down.vx v1, v1, zero\n\t"
        "vmv.v.x v16, zero\n\t"
        "vslidedown.vi v2, v1, 2\n\t"
        "vmv1r.v v3, v2\n\t"
        "vslideup.vi v2, v3, 1\n\t"
        "vsetivli zero, 2, e32, m1, ta, ma\n\t"
        "vmv.v.i v4, 4\n\t"
        "vand.vx v8, v1, %[kmask1]\n\t"
        "vslide1up.vx v5, v4, zero\n\t"
        "vsrl.vi v6, v1, 6\n\t"
        "vsrl.vv v7, v2, v5\n\t"
        "vsse32.v v8, (%[utmp]), %[s1]\n\t"
        "vand.vx v0, v6, %[kmask3]\n\t"
        "vand.vx v2, v7, %[kmask2]\n\t"
        "vsll.vi v6, v0, 4\n\t"
        "addi %[s0], %[utmp], 4\n\t"
        "vor.vv v1, v6, v2\n\t"
        "vsse32.v v1, (%[s0]), %[s1]\n\t"
        : [s0] "=&r" (s0), [s1] "=&r" (s1)
        : [utmp] "r" (utmp), [s6b] "r" (blk)
        , [kmask1] "r" (kmask1), [kmask2] "r" (kmask2), [kmask3] "r" (kmask3)
        : "memory", "v0","v1","v2","v3","v4","v5","v6","v7","v8","v16"
    );
}

// v2 P1: head comes from stack stash (as in the kernel), w2 via vmv.v.x
__attribute__((noinline))
static void p1_v2(const void * stash, uint32_t w2c, uint32_t * utmp) {
    static const uint32_t kmask1 = 0x3f3f3f3f;
    static const uint32_t kmask2 = 0x0f0f0f0f;
    static const uint32_t kmask3 = 0x03030303;
    long s0, s1, q80;
    __asm__ __volatile__(
        "li %[s1], 8\n\t"
        "addi %[q80], %[utmp], 16\n\t"
        "vsetivli zero, 4, e32, m1, ta, ma\n\t"
        "vle32.v v12, (%[q80])\n\t"
        "vmv.v.x v2, %[w2c]\n\t"
        "vslidedown.vi v1, v12, 1\n\t"
        "vsetivli zero, 2, e32, m1, ta, ma\n\t"
        "vid.v v5\n\t"
        "vsll.vi v5, v5, 2\n\t"
        "vand.vx v8, v1, %[kmask1]\n\t"
        "vsrl.vi v6, v1, 6\n\t"
        "vsrl.vv v7, v2, v5\n\t"
        "vsse32.v v8, (%[utmp]), %[s1]\n\t"
        "vand.vx v0, v6, %[kmask3]\n\t"
        "vand.vx v2, v7, %[kmask2]\n\t"
        "vsll.vi v6, v0, 4\n\t"
        "vor.vv v1, v6, v2\n\t"
        "addi %[s0], %[utmp], 4\n\t"
        "vsse32.v v1, (%[s0]), %[s1]\n\t"
        : [s0] "=&r" (s0), [s1] "=&r" (s1), [q80] "=&r" (q80)
        : [utmp] "r" (utmp), [w2c] "r" (w2c)
        , [kmask1] "r" (kmask1), [kmask2] "r" (kmask2), [kmask3] "r" (kmask3)
        : "memory", "v0","v1","v2","v3","v4","v5","v6","v7","v8","v12"
    );
}

int main(void) {
    // synthetic head: {d/dmin, w0, w1, w2} with distinct bit patterns
    const uint32_t w0 = 0x89ABCDEF, w1 = 0x01234567, w2 = 0xFEDCBA98;
    head0[0] = 0x12345678u; head0[1] = w0; head0[2] = w1; head0[3] = w2;

    uint32_t ua[8] = {0}, ub[8] = {0};
    // v2 kernel layout: the stash lives at utmp[4..7] of the SAME array
    ub[4] = head0[0]; ub[5] = w0; ub[6] = w1; ub[7] = w2;

    p1_orig(head0, ua);
    p1_v2(ub, w2, ub);   // stash = &ub[4] (q80 = utmp+16)

    printf("head: %08x %08x %08x %08x\n", head0[0], head0[1], head0[2], head0[3]);
    int ndiff = 0;
    for (int j = 0; j < 4; j++) {
        int diff = ua[j] != ub[j];
        ndiff += diff;
        printf("utmp[%d]  orig=%08x  v2=%08x %s\n", j, ua[j], ub[j], diff ? "<<<< DIFF" : "");
    }
    printf("%s (%d/4 words differ)\n", ndiff ? "P1 DECODE MISMATCH" : "P1 decode identical", ndiff);
    // also show byte view for scales/mins reading
    printf("orig bytes:"); for (int j = 0; j < 16; j++) printf(" %02x", ((uint8_t*)ua)[j]); printf("\n");
    printf("v2   bytes:"); for (int j = 0; j < 16; j++) printf(" %02x", ((uint8_t*)ub)[j]); printf("\n");
    return 0;
}
