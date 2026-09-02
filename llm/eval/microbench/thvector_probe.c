// thvector_probe.c - test whether C908 executes XTheadVector instructions
// build: -march=rv64gc_xtheadvector -static
// if SIGILL -> C908 dropped thead vector ext; if ok -> xtheadvector kernels usable
#include <stdio.h>
#include <signal.h>
#include <setjmp.h>
#include <stdint.h>

static jmp_buf jb;
static volatile sig_atomic_t got_ill = 0;

static void ill_handler(int sig) {
    (void)sig;
    got_ill = 1;
    longjmp(jb, 1);
}

int main(void) {
    signal(SIGILL, ill_handler);
    printf("probing th.vsetvli ...\n");

    if (setjmp(jb) == 0) {
        // th.vsetvli a0, a1, e8, m1, ta, ma  (xtheadvector syntax)
        register long vl_out asm("a0");
        register long avl asm("a1") = 16;
        asm volatile(
            "th.vsetvli %0, %1, e8, m1\n\t"
            : "=r"(vl_out) : "r"(avl));
        printf("th.vsetvli OK, vl=%ld\n", (long)vl_out);
    } else {
        printf("th.vsetvli -> SIGILL (not supported)\n");
        return 1;
    }

    if (setjmp(jb) == 0) {
        uint8_t buf[32] __attribute__((aligned(32)));
        asm volatile(
            "th.vsetvli zero, zero, e8, m1\n\t"
            "th.vlbu.v v1, (%0)\n\t"
            :: "r"(buf) : "v1", "memory");
        printf("th.vle8.v OK\n");
    } else {
        printf("th.vle8.v -> SIGILL\n");
        return 1;
    }

    printf("XTHEADVECTOR_SUPPORTED\n");
    return 0;
}
