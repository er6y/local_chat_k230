/* mmz.h (port) — minimal replacement for the official mpp mmz header.
 * Only the symbols tts_zh/main.cc references are declared. On big-core
 * Linux there is no mmz subsystem to init: the nncase k230 runtime's
 * mmz_allocator gets its CMA segments from mmz_shim.c
 * (kd_mpi_sys_mmz_*), and these two hooks are pure no-ops in
 * port_shim.c so the vendor call sites compile and run unchanged.
 * (shrink_memory_pool() comes from the runtime itself: util.h:590.) */
#ifndef PORT_MMZ_H
#define PORT_MMZ_H

#ifdef __cplusplus
extern "C" {
#endif

void kd_mpi_mmz_init(void);
void kd_mpi_mmz_deinit(void);

#ifdef __cplusplus
}
#endif

#endif /* PORT_MMZ_H */
