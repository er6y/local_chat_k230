/* k230_init.h — KPU boot scaffolding for the tts_zh port. */
#ifndef TTS_K230_INIT_H
#define TTS_K230_INIT_H

/* Preseed nncase's gnne_regs global via /dev/mem and call gnne_init.
 * MUST run before any kmodel is loaded (AIBase constructors). The
 * sequence is copied from kpu_gemm.cpp (ggml_kpu_init), proven on
 * board: this image's /dev/k230-gnne driver has no mmap callback, so
 * the runtime's own mapping fails and gnne_regs stays NULL — every
 * k230 subgraph then silently no-ops (zero outputs). */
void k230_kpu_init(void);

#endif /* TTS_K230_INIT_H */
