/* port_shim.c — no-op implementations of the official libmmz API surface
 * that tts_zh references. Big-core Linux needs neither: mmz segments are
 * CMA pages served through /dev/mmz by mmz_shim.c, and the vendor's
 * destructor hooks only exist to keep the call sites compiling.
 * (The real kd_mpi_sys_mmz_alloc_cached/free/flush_cache the nncase
 * k230 runtime needs are provided by llm/build/mmz_shim.c, linked in.) */
#include "mmz.h"

void kd_mpi_mmz_init(void) {}
void kd_mpi_mmz_deinit(void) {}
