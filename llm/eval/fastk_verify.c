// fastk_verify.c - verify llama-fast-k230.cpp numerics against llama.cpp
// Runs a prompt through the normal llama_decode path AND the hardcoded fast
// decoder; reports per-token max|logit diff| + argmax agreement.
#include "llama.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <time.h>

void * k230_fast_create(const struct llama_model & model, int ctx_max);
void k230_fast_destroy(void * handle);
void k230_fast_decode_one(void * handle, const struct llama_model & model,
                          int32_t token, int pos, float * logits_out);

int main(int argc, char ** argv) {
    const char * path = argc > 1 ? argv[1] : "/mnt/data/models/Qwen3-0.6B-Q8_0.gguf";
    const char * text = argc > 2 ? argv[2] : "请介绍一下中国的四大发明";
    const int n_ctx = argc > 3 ? atoi(argv[3]) : 128;
    const int n_gen = argc > 4 ? atoi(argv[4]) : 16;

    llama_model_params mp = llama_model_default_params();
    struct llama_model * model = llama_model_load_from_file(path, mp);
    if (!model) { fprintf(stderr, "model load failed\n"); return 1; }

    llama_context_params cp = llama_context_default_params();
    cp.n_ctx = n_ctx;
    cp.n_batch = 512;
    cp.n_ubatch = 512;
    cp.n_threads = 1;
    cp.n_threads_batch = 1;
    struct llama_context * ctx = llama_init_from_model(model, cp);
    if (!ctx) { fprintf(stderr, "ctx init failed\n"); return 1; }

    const int n_vocab = llama_vocab_n_tokens(llama_model_get_vocab(model));
    int32_t * toks = (int32_t *) malloc(4096 * sizeof(int32_t));
    int n_toks = llama_tokenize(llama_model_get_vocab(model), text, strlen(text), toks, 4096, true, false);
    fprintf(stderr, "prompt tokens: %d, vocab %d\n", n_toks, n_vocab);

    void * fast = k230_fast_create(*model, n_ctx);
    if (!fast) { fprintf(stderr, "fast init failed\n"); return 1; }

    float * lgt_norm = (float *) malloc((size_t) n_vocab * sizeof(float));
    float * lgt_fast = (float *) malloc((size_t) n_vocab * sizeof(float));

    // process prompt normally (prefill via the standard path)
    llama_batch b = llama_batch_init(n_toks, 0, 1);
    b.n_tokens = n_toks;
    for (int i = 0; i < n_toks; ++i) {
        b.token[i] = toks[i]; b.pos[i] = i; b.n_seq_id[i] = 1; b.seq_id[i][0] = 0;
        b.logits[i] = i == n_toks - 1;
    }
    if (llama_decode(ctx, b) != 0) { fprintf(stderr, "prefill decode failed\n"); return 1; }
    llama_batch_free(b);

    // prefill the fast path's OWN kv cache with the prompt tokens
    for (int i = 0; i < n_toks; ++i) {
        k230_fast_decode_one(fast, *model, toks[i], i, lgt_fast);
    }

    int32_t cur = toks[n_toks - 1];
    int agree = 0, total = 0;
    double worst = 0.0;
    double fast_ms_sum = 0.0, norm_ms_sum = 0.0;

    for (int step = 0; step < n_gen; ++step) {
        const int pos = n_toks + step;
        // normal decode of the current token
        struct timespec t0, t1;
        clock_gettime(CLOCK_MONOTONIC, &t0);
        b = llama_batch_init(1, 0, 1);
        b.n_tokens = 1;
        b.token[0] = cur; b.pos[0] = pos; b.n_seq_id[0] = 1; b.seq_id[0][0] = 0;
        b.logits[0] = true;
        if (llama_decode(ctx, b) != 0) { fprintf(stderr, "decode fail at %d\n", step); return 1; }
        llama_batch_free(b);
        clock_gettime(CLOCK_MONOTONIC, &t1);
        norm_ms_sum += (t1.tv_sec - t0.tv_sec) * 1e3 + (t1.tv_nsec - t0.tv_nsec) / 1e6;

        const float * lg = llama_get_logits(ctx);
        memcpy(lgt_norm, lg, (size_t) n_vocab * sizeof(float));

        clock_gettime(CLOCK_MONOTONIC, &t0);
        k230_fast_decode_one(fast, *model, cur, pos, lgt_fast);
        clock_gettime(CLOCK_MONOTONIC, &t1);
        fast_ms_sum += (t1.tv_sec - t0.tv_sec) * 1e3 + (t1.tv_nsec - t0.tv_nsec) / 1e6;

        int am_n = 0, am_f = 0;
        double md = 0.0;
        for (int j = 0; j < n_vocab; ++j) {
            const double d = fabs((double) lgt_norm[j] - (double) lgt_fast[j]);
            if (d > md) md = d;
            if (lgt_norm[j] > lgt_norm[am_n]) am_n = j;
            if (lgt_fast[j] > lgt_fast[am_f]) am_f = j;
        }
        total++;
        if (am_n == am_f) agree++;
        if (md > worst) worst = md;
        fprintf(stderr, "step %2d pos %3d tok %6d maxdiff %.6f argmax %6d/%6d %s\n",
                step, pos, cur, md, am_n, am_f, am_n == am_f ? "OK" : "DIFF");

        // greedy advance on the NORMAL path
        cur = am_n;
    }

    fprintf(stderr, "RESULT: %d/%d argmax agree, worst maxdiff %.6f\n", agree, total, worst);
    fprintf(stderr, "TIMING: fast %.1f ms/tok, normal %.1f ms/tok (n=%d)\n",
            fast_ms_sum / n_gen, norm_ms_sum / n_gen, n_gen);
    k230_fast_destroy(fast);
    llama_free(ctx);
    llama_model_free(model);
    return agree == total ? 0 : 2;
}
