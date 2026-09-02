// t_vendor.cpp - Minimal Path B M1 test: load a kmodel via vendor runtime
// and run one GEMM, compare output to daemon's output.
// Uses heap memory (like Python nncaseruntime), NOT mmz/CMA.
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <vector>
#include <memory>
#include <dlfcn.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/mman.h>
#include <gsl/gsl-lite.hpp>
#include <nncase/runtime/interpreter.h>
#include <nncase/runtime/runtime_tensor.h>
#include <nncase/runtime/host_buffer.h>

using namespace nncase;
using namespace nncase::runtime;

// mmz shim still needed for kd_mpi_sys_mmz_* (runtime may call them internally)
extern "C" int kd_mpi_sys_mmz_alloc_cached(uint64_t *phy, void **virt,
    const char *mmb, const char *zone, uint32_t len);
extern "C" void kd_mpi_sys_mmz_free(uint64_t phy, void *virt);
extern "C" void kd_mpi_sys_mmz_flush_cache(uint64_t phy, void *virt, uint32_t len);
extern "C" void gnne_init(void);

static std::vector<uint8_t> read_file(const char *path) {
    std::ifstream f(path, std::ios::binary | std::ios::ate);
    if (!f) { fprintf(stderr, "cannot open %s\n", path); exit(1); }
    auto sz = f.tellg();
    f.seekg(0);
    std::vector<uint8_t> buf(sz);
    f.read((char*)buf.data(), sz);
    return buf;
}

static std::vector<float> load_scale(const char *kmodel_path) {
    std::string p(kmodel_path);
    auto pos = p.rfind('.');
    if (pos != std::string::npos) p = p.substr(0, pos);
    p += ".scale";
    std::ifstream f(p, std::ios::binary | std::ios::ate);
    if (!f) return {};
    auto sz = f.tellg();
    f.seekg(0);
    std::vector<float> sc(sz / sizeof(float));
    f.read((char*)sc.data(), sz);
    fprintf(stderr, "loaded scale: %s (%zu floats)\n", p.c_str(), sc.size());
    return sc;
}

int main(int argc, char **argv) {
    if (argc < 4) {
        fprintf(stderr, "usage: %s <kmodel> <input.bin> <ref_output.bin>\n", argv[0]);
        return 1;
    }

    // 1. Load kmodel into heap (like Python: open().read())
    auto kmodel = read_file(argv[1]);
    size_t ksz = kmodel.size();
    fprintf(stderr, "kmodel: %zu bytes (heap @ %p)\n", ksz, kmodel.data());

    // 2. Load input + ref
    auto input_raw = read_file(argv[2]);
    size_t n_in = input_raw.size() / sizeof(float);
    float *x = (float*)input_raw.data();
    auto ref_raw = read_file(argv[3]);
    size_t n_ref = ref_raw.size() / sizeof(float);
    float *ref = (float*)ref_raw.data();
    fprintf(stderr, "input: %zu floats, ref: %zu floats\n", n_in, n_ref);

    // 3. Preseed gnne_regs + gnne_init
    {
        void **gnne_regs = (void **)dlsym(RTLD_DEFAULT, "gnne_regs");
        if (gnne_regs && !*gnne_regs) {
            int fd = open("/dev/mem", O_RDWR | O_SYNC);
            if (fd >= 0) {
                void *regs = mmap(NULL, 512, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0x80400000);
                if (regs != MAP_FAILED) {
                    *gnne_regs = regs;
                    fprintf(stderr, "gnne_regs preseeded: %p\n", regs);
                }
                close(fd);
            }
        }
        fprintf(stderr, "calling gnne_init...\n");
        gnne_init();
        fprintf(stderr, "gnne_init done\n");
    }

    // 4. Load model (copy=true, runtime copies to its own buffer)
    interpreter interp;
    auto r = interp.load_model(
        gsl::span<const gsl::byte>((const gsl::byte *)kmodel.data(), ksz), true);
    if (!r.is_ok()) { fprintf(stderr, "load_model failed\n"); return 3; }

    auto in_shape = interp.input_shape(0);
    auto out_shape = interp.output_shape(0);
    size_t K = in_shape[1], S = in_shape[2], MT = S * S;
    size_t mt = n_in / K;
    size_t OUT = out_shape[1];
    fprintf(stderr, "in=[1,%zu,%zu,%zu] out=[1,%zu,%zu,%zu] mt=%zu\n",
            K, S, S, OUT, S, S, mt);

    // 5. Fold x[mt*K] -> in[1,K,S,S] with SmoothQuant
    auto sc = load_scale(argv[1]);
    std::vector<float> inp_buf(1 * K * MT, 0.0f);
    for (size_t m = 0; m < mt; m++) {
        for (size_t k = 0; k < K; k++) {
            float scale = (k < sc.size() && sc[k] != 0.0f) ? sc[k] : 1.0f;
            inp_buf[k * MT + m] = x[m * K + k] / scale;
        }
    }

    // 6. Create input tensor from heap data (pool_cpu_only, like Python from_numpy)
    auto in_tensor_r = hrt::create(
        dt_float32,
        {1, K, S, S},
        gsl::as_writable_bytes(gsl::make_span(inp_buf)),
        true,  // copy
        hrt::pool_cpu_only
    );
    if (!in_tensor_r.is_ok()) { fprintf(stderr, "create input failed\n"); return 4; }
    auto in_tensor = in_tensor_r.unwrap();
    if (!interp.input_tensor(0, in_tensor).is_ok()) {
        fprintf(stderr, "set_input failed\n"); return 5;
    }

    // 7. Run (let runtime handle cache coherence internally)
    fprintf(stderr, "running...\n");
    if (!interp.run().is_ok()) { fprintf(stderr, "run failed\n"); return 7; }

    // 8. Get output (to_host + map, like Python to_numpy)
    auto out_tensor_r = interp.output_tensor(0);
    if (!out_tensor_r.is_ok()) { fprintf(stderr, "get_output failed\n"); return 8; }
    auto out_tensor = out_tensor_r.unwrap();

    auto to_host_r = out_tensor.to_host();
    if (!to_host_r.is_ok()) { fprintf(stderr, "to_host failed\n"); return 9; }
    auto host_tensor = to_host_r.unwrap();

    auto map_r = hrt::map(host_tensor, map_read);
    if (!map_r.is_ok()) { fprintf(stderr, "map failed\n"); return 10; }
    auto mapped = std::move(map_r).unwrap();
    const float *out_data = (const float*)mapped.buffer().data();

    fprintf(stderr, "out[0:8]: ");
    for (size_t i = 0; i < 8; i++) fprintf(stderr, "%.4f ", out_data[i]);
    fprintf(stderr, "\nref[0:8]: ");
    for (size_t i = 0; i < 8; i++) fprintf(stderr, "%.4f ", ref[i]);
    fprintf(stderr, "\n");

    // 9. Compare y[mt,OUT] from out[1,OUT,S,S]
    float max_rel = 0, max_abs = 0;
    size_t mismatches = 0;
    double dot = 0, na = 0, nb = 0;
    for (size_t m = 0; m < mt; m++) {
        for (size_t o = 0; o < OUT; o++) {
            float y = out_data[o * MT + m];
            float rv = ref[m * OUT + o];
            float diff = fabsf(y - rv);
            float rel = fabsf(rv) > 1e-6f ? diff / fabsf(rv) : diff;
            if (rel > max_rel) max_rel = rel;
            if (diff > max_abs) max_abs = diff;
            if (rel > 0.01f) mismatches++;
            dot += (double)y * rv;
            na += (double)y * y;
            nb += (double)rv * rv;
        }
    }
    float cos = (float)(dot / (sqrt(na) * sqrt(nb) + 1e-30));

    fprintf(stderr, "\n=== RESULT ===\n");
    fprintf(stderr, "mt=%zu K=%zu OUT=%zu\n", mt, K, OUT);
    fprintf(stderr, "max_abs_err = %g\n", max_abs);
    fprintf(stderr, "max_rel_err = %g\n", max_rel);
    fprintf(stderr, "mismatches(>1%%) = %zu / %zu\n", mismatches, mt * OUT);
    fprintf(stderr, "cosine = %.6f\n", cos);
    fprintf(stderr, "cos>0.98: %s\n", cos > 0.98f ? "PASS" : "FAIL");

    return cos > 0.98f ? 0 : 1;
}
