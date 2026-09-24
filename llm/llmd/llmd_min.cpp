// llmd_min.cpp — KPU local 崩溃二分用最小复现体
// 只做: 参数解析 → ggml_backend_load_all → kpu_maybe_init → 退出
#include "llama.h"
#include "common.h"
#include <cstdio>
#include <cstring>
#include <cstdlib>

#if defined(__riscv)
extern "C" void ggml_kpu_init(const char * dir);
static void kpu_maybe_init() {
    const char * kd = getenv("KPU_KMODEL_DIR");
    if (!kd) kd = "/mnt/data/kpu_qwen";
    fprintf(stderr, "[min] kpu_maybe_init dir=%s\n", kd);
    ggml_kpu_init(kd);
    fprintf(stderr, "[min] kpu init returned\n");
}
#else
static void kpu_maybe_init() {}
#endif

int main(int argc, char ** argv) {
    const char * model_path = "";
    bool use_kpu = false;
    for (int i = 1; i < argc; i++) {
        if (!strcmp(argv[i], "--model")) model_path = argv[++i];
        else if (!strcmp(argv[i], "--kpu")) use_kpu = true;
    }
    fprintf(stderr, "[min] start model=%s kpu=%d\n", model_path, (int)use_kpu);
    ggml_backend_load_all();
    fprintf(stderr, "[min] backends loaded\n");
    if (use_kpu) kpu_maybe_init();
    fprintf(stderr, "[min] done, exiting cleanly\n");
    return 0;
}
