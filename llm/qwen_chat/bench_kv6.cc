// bench_kv6: C++ timing harness for kv6_stacked 5-input decode kmodel
// usage: ./bench_kv6 <model_path> [reps]
#include <cstdio>
#include <cstring>
#include <random>
#include <vector>
#include <chrono>
#include <algorithm>
#include <fstream>
#include <memory>

#include <nncase/runtime/simple_types.h>
#include <nncase/runtime/runtime_op_utility.h>

#include "nncasewrapper.hpp"

using namespace Ort;

static std::mt19937 rng(9);
static std::normal_distribution<float> nd(0.f, 0.3f);

static std::shared_ptr<RuntimeManager> rtmgr;

template <typename T>
static nncase::value_t mk_tensor(const std::vector<int> &shape, bool random, float scale) {
    auto t = _Input<T>(shape, rtmgr);
    auto buf = t->buffer().as_host().unwrap_or_throw();
    auto mapped = buf.map(nncase::runtime::map_write).unwrap_or_throw();
    size_t n = 1;
    for (int d : shape)
        n *= d;
    T *p = reinterpret_cast<T *>(mapped.buffer().data());
    for (size_t i = 0; i < n; i++) {
        if constexpr (std::is_same_v<T, float>)
            p[i] = random ? nd(rng) * scale : 0.f;
        else
            p[i] = static_cast<T>(21);
    }
    return t;
}

int main(int argc, char **argv) {
    if (argc < 2) {
        fprintf(stderr, "usage: %s <model> [reps]\n", argv[0]);
        return 1;
    }
    int reps = argc > 2 ? atoi(argv[2]) : 20;
    std::shared_ptr<RuntimeManager> runtime_manager(new RuntimeManager());
    Module module(runtime_manager, argv[1]);
    fprintf(stderr, "loaded %s\n", argv[1]);

    std::vector<nncase::value_t> inputs;
    const char *mname = strrchr(argv[1], '/');
    bool official = mname && strstr(mname, "llm.kmodel");
    if (official) {
        inputs.push_back(mk_tensor<float>({1, 1, 896}, true, 1.0f));
        inputs.push_back(mk_tensor<float>({1, 1, 1, 1}, false, 0.f));
        inputs.push_back(mk_tensor<int32_t>({1, 1}, false, 0.f));
        int H = argc > 3 ? atoi(argv[3]) : 255;
        inputs.push_back(mk_tensor<float>({24, 2, 1, H, 2, 64}, true, 0.66f));
    } else {
        inputs.push_back(mk_tensor<float>({1, 1, 896}, true, 1.0f));
        inputs.push_back(mk_tensor<float>({1, 1, 1, 33}, false, 0.f));
        inputs.push_back(mk_tensor<int32_t>({1, 1}, false, 0.f));
        inputs.push_back(mk_tensor<float>({48, 64, 32}, true, 0.66f));
        inputs.push_back(mk_tensor<float>({48, 32, 64}, true, 0.66f));
    }

    // warmup
    for (int i = 0; i < 3; i++) {
        auto out = module.onForward(inputs);
    }
    std::vector<double> ts;
    for (int i = 0; i < reps; i++) {
        auto t0 = std::chrono::steady_clock::now();
        auto out = module.onForward(inputs);
        auto t1 = std::chrono::steady_clock::now();
        ts.push_back(std::chrono::duration<double, std::milli>(t1 - t0).count());
    }
    std::sort(ts.begin(), ts.end());
    double mean = 0;
    for (size_t i = 2; i + 2 < ts.size(); i++)
        mean += ts[i];
    mean /= (ts.size() > 4 ? ts.size() - 4 : 1);
    fprintf(stderr, "CPP_BENCH mean=%.2fms min=%.2f max=%.2f (n=%zu)\n", mean, ts.front(), ts.back(), ts.size());
    printf("CPP_BENCH mean=%.2fms min=%.2f max=%.2f\n", mean, ts.front(), ts.back());
    return 0;
}
