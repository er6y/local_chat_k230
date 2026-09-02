// mmz_shim.c - kd_mpi_sys_mmz_* reimplementation for nncase k230 linux runtime
// reversed from official face_detect.elf (libsys.a static part):
//   entry(40B): virt@0 rsv@8 phy@16 len@24 next@32
//   ioctl alloc=0xC0086701 free=0xC0086702 (_IOWR('g',1/2,8)),
//   built as lui 0x60043 + slli 1 + addi 0x701/0x702 -- NOTE: objdump's
//   immediate-track comment (60043701) is WRONG, it ignores the slli.
//   open("/dev/mmz", 0x101002)
//   flush_cache: fence + th.dcache.civa (0x0275800b) loop, 64B lines
//   official code does NOT check ioctl return; kernel fills e->phy.
// NOTE ABI (as called by nncase v2.11 libfunctional_k230.a):
//   alloc_cached(k_u64* phy, void** virt, const char* mmb, const char* zone, k_u32 len)
//   free(void* virt)            <- only a0 set at call site
//   flush_cache(k_u64 phy, void* virt, k_u32 len)  <- uses a1/a2
//
// ---- leak fix v4: fully TRANSPARENT shim + nncase-side free ---------------
// The nncase k230 runtime wraps every mmz segment into an arena with a
// 16-byte-per-block free-list heap of its own (mmz_allocator, reversed via
// objdump: this+0=avail_bytes_, this+8=free list, this+16=mutex; a static
// global instance lives at buffer_allocator::host()+8). Its allocate() only
// hits the kernel when its free list can't serve the request, and
// try_free_segment() even returns fully-idle arenas to the kernel via
// kd_mpi_sys_mmz_free. The leak happens because the C API invoke path never
// feeds invoke results back into that heap.
// Two lessons learned the hard way (both segfault/assert during preload):
//  - never reclaim/munmap segments behind nncase's back (it keeps stale
//    references and dereferences them on the next kmodel load)
//  - never serve allocs from a private pool (breaks avail_bytes_ == free
//    list accounting -> sanity_check assertion abort)
// So this shim stays perfectly transparent (alloc==ioctl, free==ioctl) and
// kpu_gemm.cpp closes the loop on nncase's side instead: after copying y
// out it calls mmz_allocator::free(host+8, result_ptr) directly, which
// puts the block back on nncase's own free list - subsequent invokes then
// reuse it in-heap with zero kernel traffic.
#include <stdint.h>
/* mmap-offset diagnostic REMOVED: the popen() fork it used destabilized the
 * process (early SIGSEGV right after the first cache flush) */
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <unistd.h>
#include <dlfcn.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/mman.h>
#include <sys/ioctl.h>
#include <errno.h>
#include <pthread.h>

static int fd_mmz = -1;
static pthread_mutex_t mmz_mutex = PTHREAD_MUTEX_INITIALIZER;
// MMZ_TRACE=1: log every alloc/free with segment identity (phy addr).
static int mmz_trace = -1;
static long mmz_flush_calls = 0;   // diagnostics: how many real cache flushes
#define TRC(...) do { if (mmz_trace < 0) mmz_trace = getenv("MMZ_TRACE") ? 1 : 0; \
                      if (mmz_trace) fprintf(stderr, __VA_ARGS__); } while (0)

typedef struct mmz_node {
    void *virt;              // 0
    uint64_t rsv;            // 8
    uint64_t phy;            // 16
    uint64_t len;            // 24
    struct mmz_node *next;   // 32
} mmz_node_t;                // 40 bytes

static mmz_node_t *plist = NULL;
static long mmz_free_calls = 0;   // diagnostic: how many real frees nncase did
static int  mmz_heal_count = 0;   // drop_caches+compact self-heal budget:
                                  // system() under mutex can stall the whole
                                  // process for seconds; cap it at 2 so a dirty
                                  // pool degrades gracefully (caller falls back
                                  // to CPU) instead of hanging the board.

// last-resort cleanup: nncase leaks mmz segments on exit (CmaFree stays low
// after process death -> next process aborts at alloc). Walk our own list and
// return everything to the kernel. Idempotent: free() unlinks nodes.
static void mmz_atexit_release_all(void) {
    int n = 0;
    while (plist) {
        mmz_node_t *e = plist;
        plist = e->next;
        munmap(e->virt, (size_t)e->len);
        ioctl(fd_mmz, 0xC0086702UL, e);
        free(e);
        n++;
    }
    if (n || mmz_free_calls)
        fprintf(stderr, "mmz_shim: exit released %d leftover segs (nncase freed %ld)\n",
                n, mmz_free_calls);
}

int kd_mpi_sys_mmz_alloc_cached(uint64_t *phy_addr, void **virt_addr,
                                const char *mmb, const char *zone, uint32_t len) {
    (void)mmb; (void)zone;
    pthread_mutex_lock(&mmz_mutex);
    if (fd_mmz < 0) {
        fd_mmz = open("/dev/mmz", 0x101002 /* O_RDWR|O_SYNC as vendor */);
        if (fd_mmz < 0) { fprintf(stderr, "mmz_shim: open /dev/mmz failed\n"); pthread_mutex_unlock(&mmz_mutex); return -1; }
        atexit(mmz_atexit_release_all);
    }
    mmz_node_t *e = (mmz_node_t*)calloc(1, sizeof(*e));
    if (!e) { pthread_mutex_unlock(&mmz_mutex); return -1; }
    e->len = len;
    int iorc = ioctl(fd_mmz, 0xC0086701UL, e);
    if (e->phy == 0 && mmz_heal_count < 2) {
        // CMA fragmentation self-heal (root only): reclaim page-cache pages
        // borrowed from the CMA window, compact, then retry the alloc once.
        // Bounded by mmz_heal_count: a badly fragmented pool must fail fast
        // (callers degrade to CPU) rather than loop expensive compaction.
        mmz_heal_count++;
        if (system("echo 3 > /proc/sys/vm/drop_caches 2>/dev/null; "
                   "echo 1 > /proc/sys/vm/compact_memory 2>/dev/null") != -1) {
            uint64_t keep_len = e->len;
            memset(e, 0, sizeof(*e));
            e->len = keep_len;
            iorc = ioctl(fd_mmz, 0xC0086701UL, e);
        }
    }
    if (e->phy == 0) {
        fprintf(stderr, "mmz_shim: ioctl alloc failed (phy=0) len=%u rc=%d errno=%d(%s) tid=%lu\n",
                len, iorc, errno, strerror(errno), (unsigned long)pthread_self());
        free(e); pthread_mutex_unlock(&mmz_mutex); return -1;
    }
    void *v = mmap(NULL, len, PROT_READ | PROT_WRITE, MAP_SHARED, fd_mmz, e->phy);
    if (v == MAP_FAILED) {
        fprintf(stderr, "mmz_shim: mmap failed len=%u ioctl_phy=0x%llx\n", len, (unsigned long long)e->phy);
        free(e); pthread_mutex_unlock(&mmz_mutex); return -1;
    }
    e->virt = v;
    *phy_addr = e->phy;
    *virt_addr = v;
    TRC("[mmz] A len=%u phy=%llx v=%p\n", len, (unsigned long long)e->phy, v);
#ifdef MMZ_DIAG_MAP
    // one-shot diagnostic: write a magic through the CPU mapping, then read
    // the SAME physical address back via devmem. If they differ, our mmap
    // offset (e->phy) does not actually select this segment and every KPU
    // transfer in the process has been reading a different block.
    if (len >= 64) {
        *(volatile uint32_t *)((char *)v + 32) = 0xCAFEBABE;
        __asm__ volatile("fence" ::: "memory");
        char cmd[96];
        snprintf(cmd, sizeof cmd, "devmem 0x%llx 32",
                 (unsigned long long)(e->phy + 32));
        FILE *pp = popen(cmd, "r");
        if (pp) {
            char line[64] = "";
            if (fgets(line, sizeof line, pp))
                fprintf(stderr, "[mmz] DIAG phy=0x%llx+32 cpu=0xCAFEBABE devmem=%s",
                        (unsigned long long)e->phy, line);
            pclose(pp);
        }
    }
#endif
    if (!plist) plist = e;
    else { mmz_node_t *p = plist; while (p->next) p = p->next; p->next = e; }
    pthread_mutex_unlock(&mmz_mutex);
    return 0;
}

int kd_mpi_sys_mmz_free(uint64_t phy_addr, void *virt_addr) {
    (void)phy_addr;
    pthread_mutex_lock(&mmz_mutex);
    mmz_node_t **pp = &plist;
    while (*pp && (*pp)->virt != virt_addr) pp = &(*pp)->next;
    mmz_node_t *e = *pp;
    if (!e) { pthread_mutex_unlock(&mmz_mutex); return -1; }
    TRC("[mmz] F phy=%llx v=%p\n", (unsigned long long)e->phy, e->virt);
    munmap(e->virt, (size_t)e->len);
    ioctl(fd_mmz, 0xC0086702UL, e);
    *pp = e->next;
    free(e);
    mmz_free_calls++;
    pthread_mutex_unlock(&mmz_mutex);
    return 0;
}

int kd_mpi_sys_mmz_flush_cache(uint64_t phy_addr, void *virt_addr, uint32_t len) {
    (void)phy_addr;
    // REAL flush. The coherence probe (cpu magic vs /dev/mem readback at the
    // same phys) proved these mappings are CACHED: without cleaning the lines
    // the KPU reads stale DDR (the months-long zero-output root cause). This
    // is what the vendor libsys did before we no-op'd it: th.dcache.civa
    // (clean+invalidate) over 64B lines, then a full fence.
    if (virt_addr && len) {
        uintptr_t a = (uintptr_t)virt_addr & ~(uintptr_t)63;
        uintptr_t e = (uintptr_t)virt_addr + len;
        for (; a < e; a += 64)
            __asm__ volatile("th.dcache.civa %0" :: "r"(a) : "memory");
        __asm__ volatile("fence rw, rw" ::: "memory");
    }
    return 0;
}

// ---- GNNE call tracer (link with -Wl,--wrap=gnne_*) --------------------------
// Answers definitively whether the 2.9 k230 execution path ever reaches the
// GNNE bring-up calls. Set GNNE_QUIET=1 to silence.
extern uint64_t __real_gnne_enable(uint64_t, uint64_t, uint64_t);
extern uint64_t __real_gnne_ctrl_set(uint64_t);
extern uint64_t __real_gnne_disable(void);
static int gnne_trace = -1;
// fflush+fsync: diagnostic lines must survive the hard WDT resets this
// debugging tends to trigger (ext4 journal otherwise rolls the log back)
#define GTRC(...) do { if (gnne_trace < 0) gnne_trace = getenv("GNNE_QUIET") ? 0 : 1; \
                       if (gnne_trace) { fprintf(stderr, __VA_ARGS__); \
                                         fflush(stderr); fsync(fileno(stderr)); } } while (0)

// cpu vaddr -> phys addr. 1) our tracked mmz segments (authoritative, and the
// only memory the GNNE can DMA from: the 1GB CMA window at 0x3fe00000).
// 2) /proc/self/pagemap fallback (root) for anything else.
static uint64_t cpu_to_phys(uint64_t v) {
    pthread_mutex_lock(&mmz_mutex);
    for (mmz_node_t *e = plist; e; e = e->next)
        if (v >= (uint64_t)e->virt && v < (uint64_t)e->virt + e->len) {
            uint64_t r = e->phy + (v - (uint64_t)e->virt);
            pthread_mutex_unlock(&mmz_mutex);
            return r;
        }
    pthread_mutex_unlock(&mmz_mutex);
    int fd = open("/proc/self/pagemap", O_RDONLY);
    if (fd < 0) return 0;
    uint64_t ent = 0, r = 0;
    if (pread(fd, &ent, 8, (off_t)((v >> 12) * 8)) == 8 && (ent & (1ULL << 63))) {
        uint64_t pfn = ent & ((1ULL << 55) - 1);
        if (pfn) r = (pfn << 12) | (v & 0xfff);
    }
    close(fd);
    return r;
}

// exported for kpu_gemm.cpp diagnostics: is this pointer mmz(CMA)-backed?
uint64_t mmz_cpu_to_phys(uint64_t v) { return cpu_to_phys(v); }

// coherence probe: read a word at PHYS via /dev/mem (bypasses any CPU cache)
uint32_t mmz_phys_read32(uint64_t phy) {
    int fd = open("/dev/mem", O_RDWR | O_SYNC);
    if (fd < 0) return 0xdeadbeef;
    void *p = mmap(NULL, 0x1000, PROT_READ | PROT_WRITE, MAP_SHARED,
                   fd, (off_t)(phy & ~0xfffULL));
    close(fd);
    if (p == MAP_FAILED) return 0xdeadbeef;
    uint32_t v = *(volatile uint32_t *)((char *)p + (phy & 0xfff));
    munmap(p, 0x1000);
    return v;
}

// THE missing link: both the 2.9 and 2.11 linux runtimes pass the GNNE program
// counter as a plain CPU VIRTUAL pointer (objdump: invoke_core loads this+80,
// set by initialize_core as module_data_ptr + offset). On RT-Smart (small core)
// virtual==physical so nobody noticed; on Linux the GNNE fetches via its own
// bus/MMU window over the CMA pool (0x3fe00000+1GB) and a CPU pointer is
// unfetchable -> enable "succeeds", nothing executes, outputs stay zero.
// Translate pc_s/pc_e to physical before the real enable. GNNE_L2P=0 disables.
static int gnne_l2p = -1;
// ---- L2 SRAM program staging ---------------------------------------------------
// Even with a physical PC the core never started: pc_s/pc_e are virtually
// contiguous but land in TWO different (non-adjacent, out-of-order) CMA
// segments - the fetcher cannot walk that. gnne.h spells out the intended
// design: L2_BASE_ADDR 0x80000000, gnne_get_l2v() returns it - programs are
// meant to run from the KPU-dedicated 2MB L2 SRAM. Copy the program range
// (from the process's own virtual memory) into L2 and enable with L2
// addresses. GNNE_L2STAGE=0 disables.
static int gnne_l2stage = -1;
static unsigned char *l2win = NULL;
static uint64_t l2_off = 0x1000;   // keep L2 page 0 untouched

// CMA staging (the working hypothesis): L2 SRAM reads ZERO from the big core
// (small-core owned), so stage the program into a dedicated CONTIGUOUS mmz
// segment and run from its PHYSICAL address. The program's original pc range
// can span two mmz allocations (virt-contiguous, phys-not), so a private
// segment also guarantees physical contiguity.
static int gnne_cstage = -1;
static unsigned char *cstage_virt = NULL;
static uint64_t cstage_phy = 0;
#define CSTAGE_SZ (1u << 20)
static uint64_t gnne_cma_stage(uint64_t pc_s, uint64_t pc_e) {
    uint64_t len = pc_e - pc_s;
    if (!len || len > CSTAGE_SZ) { GTRC("[gnne] cstage bad len %llu\n", (unsigned long long)len); return 0; }
    if (!cstage_virt) {
        if (kd_mpi_sys_mmz_alloc_cached(&cstage_phy, (void **)&cstage_virt,
                                        NULL, NULL, CSTAGE_SZ) != 0 || !cstage_virt) {
            GTRC("[gnne] cstage alloc failed\n");
            return 0;
        }
        GTRC("[gnne] cstage seg phys=0x%llx len=1MB\n", (unsigned long long)cstage_phy);
    }
    memcpy(cstage_virt, (const void *)(uintptr_t)pc_s, (size_t)len);
    __asm__ volatile("fence rw, rw" ::: "memory");
    // ---- descriptor repair: the runtime patches data addresses into the
    // program as CPU VIRTUAL pointers (RT-Smart heritage). Rewrite every word
    // that falls inside one of our mmz segments' virtual range to the matching
    // PHYSICAL address, so the GNNE actually reaches the tensors/weights.
    {
        int p32 = 0, p64 = 0;
        pthread_mutex_lock(&mmz_mutex);
        for (mmz_node_t *e = plist; e; e = e->next) {
            uint32_t vlo = (uint32_t)(uint64_t)e->virt;
            uint32_t vhi = (uint32_t)((uint64_t)e->virt >> 32);
            uint64_t seglen = e->len;
            for (uint32_t i = 0; i + 8 <= (uint32_t)len; i += 4) {
                uint32_t w = *(uint32_t *)(cstage_virt + i);
                if (w >= vlo && (uint64_t)(w - vlo) < seglen) {
                    uint64_t off = w - vlo;
                    uint32_t phyl = (uint32_t)(e->phy + off);
                    uint32_t *slot = (uint32_t *)(cstage_virt + i);
                    if (slot[1] == vhi) {           // full 64-bit virt pointer
                        slot[0] = phyl; slot[1] = 0;
                        p64++;
                    } else {                        // truncated 32-bit pointer
                        *slot = phyl;
                        p32++;
                    }
                }
            }
        }
        pthread_mutex_unlock(&mmz_mutex);
        if (p32 || p64)
            GTRC("[gnne] descriptor repair: %d x32bit + %d x64bit virt->phys\n", p32, p64);
    }
    FILE *f = fopen("/mnt/data/gnne_prog.bin", "wb");   // keep for manual kicks
    if (f) { fwrite(cstage_virt, 1, (size_t)len, f); fclose(f); }
    GTRC("[gnne] cstage %lluB v0x%llx -> phys 0x%llx\n",
         (unsigned long long)len, (unsigned long long)pc_s,
         (unsigned long long)cstage_phy);
    return cstage_phy;
}

static uint64_t gnne_l2_stage(uint64_t pc_s, uint64_t pc_e) {
    if (!l2win) {
        int fd = open("/dev/mem", O_RDWR | O_SYNC);
        if (fd >= 0) {
            l2win = (unsigned char *)mmap(NULL, 2 * 1024 * 1024,
                                          PROT_READ | PROT_WRITE, MAP_SHARED,
                                          fd, 0x80000000ULL);
            close(fd);
        }
        if (!l2win || l2win == (unsigned char *)MAP_FAILED) {
            GTRC("[gnne] L2 map FAILED\n");
            l2win = NULL;
            return 0;
        }
    }
    uint64_t len = pc_e - pc_s;
    if (!len || len > 1024 * 1024) {   // sanity: L2 is 2MB, stay under 1MB
        GTRC("[gnne] L2 stage: bad len %llu\n", (unsigned long long)len);
        return 0;
    }
    memcpy(l2win + l2_off, (const void *)(uintptr_t)pc_s, (size_t)len);
    __asm__ volatile("fence rw, rw" ::: "memory");
    uint64_t a = 0x80000000ULL + l2_off;
    GTRC("[gnne] L2 staged %llu B from v0x%llx -> 0x%llx..0x%llx\n",
         (unsigned long long)len, (unsigned long long)pc_s,
         (unsigned long long)a, (unsigned long long)(a + len));
    return a;
}

// gnne_regs lives in gnne.c.o (whole-archive); grab it for direct status reads
static volatile uint64_t *gnne_regwin(void) {
    static volatile uint64_t *win;
    if (!win) {
        void **p = (void **)dlsym(RTLD_DEFAULT, "gnne_regs");
        if (p) win = (volatile uint64_t *)*p;
    }
    return win;
}
// gnne_reg_file layout (gnne.h, base offset F0 from 0x80400000):
// icache@+0, pc@+16/20/24, ctrl@+56(0x38), status@+64(0x40), dec_ld_st_mfu_pc@+72
static void gnne_snap(const char *tag) {
    volatile uint64_t *w = gnne_regwin();
    if (!w) { GTRC("[gnne] %s: no regwin\n", tag); return; }
    uint64_t st = w[8];        // (0xf0+0x40)/8 = 8
    uint64_t pcs = w[9];       // dec_pc|load_pc
    GTRC("[gnne] %s: status=0x%llx dec_pc=0x%llx\n", tag,
         (unsigned long long)st, (unsigned long long)pcs);
}
uint64_t __wrap_gnne_enable(uint64_t pc_s, uint64_t pc_e, uint64_t pc_bp) {
    // KPU_LOCAL=1: pass-through to real gnne_enable (vendor runtime handles
    // its own buffer management; cstage/l2stage would corrupt heap-backed
    // kmodel buffers). M1 proved the raw path works with heap + copy=true.
    if (getenv("KPU_LOCAL") && atoi(getenv("KPU_LOCAL"))) {
        GTRC("[gnne] local mode: pass-through enable\n");
        return __real_gnne_enable(pc_s, pc_e, pc_bp);
    }
    if (gnne_l2p < 0) gnne_l2p = getenv("GNNE_L2P") ? atoi(getenv("GNNE_L2P")) : 1;
    if (gnne_l2stage < 0) gnne_l2stage = getenv("GNNE_L2STAGE") ? atoi(getenv("GNNE_L2STAGE")) : 0;
    if (gnne_cstage < 0) gnne_cstage = getenv("GNNE_CSTAGE") ? atoi(getenv("GNNE_CSTAGE")) : 1;
    uint64_t ps = 0, pe = 0, span = pc_e > pc_s ? pc_e - pc_s : 0;
    if (gnne_cstage && span && span <= CSTAGE_SZ) {
        ps = gnne_cma_stage(pc_s, pc_e);
        if (ps) pe = ps + span;
    }
    if (!ps && gnne_l2stage && span && span <= 1024 * 1024) {
        ps = gnne_l2_stage(pc_s, pc_e);
        if (ps) pe = ps + span;
    }
    if (!ps && gnne_l2p) { ps = cpu_to_phys(pc_s); pe = cpu_to_phys(pc_e); }
    GTRC("[gnne] enable pc_s v=0x%llx p=0x%llx | pc_e v=0x%llx p=0x%llx%s\n",
         (unsigned long long)pc_s, (unsigned long long)ps,
         (unsigned long long)pc_e, (unsigned long long)pe,
         (ps && pe) ? "" : " (translate FAILED, raw)");
    uint64_t r = __real_gnne_enable(ps ? ps : pc_s, pe ? pe : pc_e, pc_bp);
    GTRC("[gnne] enable -> %llu\n", (unsigned long long)r);
    // execution-window snapshot (GNNE_SNAP=1): costs ~50ms per invoke, off by
    // default so benches stay honest
    if (getenv("GNNE_SNAP")) {
        for (int i = 0; i < 4; i++) {
            static const int del[] = {100, 900, 4000, 45000};  // us -> ~0.1/1/5/50ms
            usleep(del[i]);
            char tag[16];
            snprintf(tag, sizeof tag, "+%dms", (del[i] + 500) / 1000);
            gnne_snap(tag);
        }
    }
    return r;
}

// The image's /dev/k230-gnne has NO IRQ registered (PLIC173 belongs to USB on
// this kernel) - the runtime's poll() can never see a completion interrupt.
// Busy-wait on the status register instead, then report ready.
#include <poll.h>
int poll(struct pollfd *fds, nfds_t nfds, int timeout) {
    static int (*real_poll)(struct pollfd *, nfds_t, int);
    if (!real_poll) real_poll = (int (*)(struct pollfd *, nfds_t, int))
        dlsym(RTLD_NEXT, "poll");
    int is_gnne = 0;
    if (nfds == 1 && fds[0].fd >= 0) {
        char link[48], target[160];
        snprintf(link, sizeof link, "/proc/self/fd/%d", fds[0].fd);
        ssize_t n = readlink(link, target, sizeof target - 1);
        if (n > 0) { target[n] = 0; is_gnne = !!strstr(target, "k230-gnne"); }
    }
    if (is_gnne) {
        volatile uint64_t *w = gnne_regwin();
        if (w) {
            // wait for work_status(bits14-15) to leave RUNNING(1) back to IDLE(0),
            // bounded like the hardware timeout (200000 units ~ 200ms)
            for (int i = 0; i < 200000; i++) {
                uint64_t st = w[8];
                if (((st >> 14) & 3) != 1) {          // not RUNNING anymore
                    fds[0].revents = POLLIN;
                    GTRC("[gnne] poll: done at iter %d status=0x%llx\n", i,
                         (unsigned long long)st);
                    return 1;
                }
                for (int v = 0; v < 50; v++) __asm__ volatile("");
            }
            fds[0].revents = POLLIN;
            GTRC("[gnne] poll: TIMEOUT busy-wait, status still RUNNING\n");
            return 1;
        }
    }
    return real_poll(fds, nfds, timeout);
}
uint64_t __wrap_gnne_ctrl_set(uint64_t v) {
    GTRC("[gnne] ctrl_set(0x%llx)\n", (unsigned long long)v);
    return __real_gnne_ctrl_set(v);
}
uint64_t __wrap_gnne_disable(void) {
    GTRC("[gnne] disable()\n");
    uint64_t r = __real_gnne_disable();
    GTRC("[gnne] disable -> %llu\n", (unsigned long long)r);
    return r;
}

// ---- mmap interceptor: rescue the GNNE register mapping ---------------------
// This image's /dev/k230-gnne driver has NO mmap callback (every parameter
// combination returns ENODEV), but the nncase k230 runtime needs to map the
// GNNE register block at 0x80400000 (mmap(NULL, 512, RW, SHARED, fd, 0x80400000),
// objdump-verified) to program the KPU. /dev/mem maps the exact same physical
// addresses fine, so when a mmap on a k230 char dev fails we transparently
// retry through /dev/mem with the identical offset. Without this the KPU has
// NEVER run: gnne_regs stays NULL, every k230 subgraph silently no-ops and
// outputs stay zero (the entire garbled-output saga since day one).
void *mmap(void *addr, size_t len, int prot, int flags, int fd, off_t off) {
    static void *(*real_mmap)(void *, size_t, int, int, int, off_t);
    if (!real_mmap) real_mmap = (void *(*)(void *, size_t, int, int, int, off_t))
        dlsym(RTLD_NEXT, "mmap");
    void *r = real_mmap ? real_mmap(addr, len, prot, flags, fd, off) : MAP_FAILED;
    if (r == MAP_FAILED && fd >= 0) {
        char link[48], target[160];
        snprintf(link, sizeof link, "/proc/self/fd/%d", fd);
        ssize_t n = readlink(link, target, sizeof target - 1);
        if (n > 0) {
            target[n] = 0;
            if (strstr(target, "k230-gnne") || strstr(target, "k230-ai2d")) {
                int mfd = open("/dev/mem", O_RDWR | O_SYNC);
                if (mfd >= 0) {
                    r = real_mmap(addr, len, prot, flags, mfd, off);
                    int saved = errno;
                    close(mfd);
                    errno = saved;
                    if (r != MAP_FAILED)
                        fprintf(stderr, "[mmz] mmap rescue: %s off=0x%llx len=%zu "
                                        "-> /dev/mem %p\n", target,
                                (unsigned long long)off, len, r);
                }
            }
        }
    }
    return r;
}
