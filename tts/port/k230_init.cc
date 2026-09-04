/* k230_init.cc — gnne_regs preseed + gnne_init, local (in-process) mode.
 * Copied from the proven sequence in kpu_gemm.cpp / ggml_kpu_init(). */
#include "k230_init.h"

#include <dlfcn.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <unistd.h>
#include <cstdio>

extern "C" void gnne_init(void);

void k230_kpu_init(void) {
    void **gnne_regs = (void **)dlsym(RTLD_DEFAULT, "gnne_regs");
    if (gnne_regs && !*gnne_regs) {
        int mfd = open("/dev/mem", O_RDWR | O_SYNC);
        if (mfd >= 0) {
            void *regs = mmap(NULL, 0x1000, PROT_READ | PROT_WRITE,
                              MAP_SHARED, mfd, 0x80400000ULL);
            close(mfd);
            if (regs != MAP_FAILED) {
                *gnne_regs = regs;
                fprintf(stderr, "[tts] gnne_regs preseeded via /dev/mem: %p\n", regs);
            } else {
                fprintf(stderr, "[tts] WARN: gnne_regs /dev/mem mmap failed\n");
            }
        } else {
            fprintf(stderr, "[tts] WARN: cannot open /dev/mem for gnne_regs\n");
        }
    } else if (gnne_regs && *gnne_regs) {
        fprintf(stderr, "[tts] gnne_regs already set: %p\n", *gnne_regs);
    } else {
        fprintf(stderr, "[tts] WARN: gnne_regs symbol not found (runtime linked without rt_modules.k230?)\n");
    }

    fprintf(stderr, "[tts] calling gnne_init...\n");
    gnne_init();
    fprintf(stderr, "[tts] gnne_init done\n");
}
