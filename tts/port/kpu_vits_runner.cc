/*
 * kpu_vits_runner.cc — KPU-accelerated VITS TTS runner for K230.
 *
 * Architecture:
 *   Text -> TextNormalizer (FST) -> Lexicon (G2P) -> Token IDs
 *   Token IDs -> enc_dp.onnx (CPU, ORT) -> mu [1,96,N], logs [1,96,N], logw [1,1,N]
 *   Length Regulator (CPU, RVV) -> z [1,96,F]
 *   z Chunking (512 frames) -> subgen.kmodel (KPU, nncase) -> audio chunks
 *   Audio Chunks -> 8kHz PCM16 WAV
 */

#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <ctime>
#include <fstream>
#include <iostream>
#include <memory>
#include <random>
#include <sstream>
#include <string>
#include <unordered_map>
#include <map>
#include <vector>
#include <unistd.h>
#include <sys/resource.h>

#include <nncase/runtime/interpreter.h>
#include <nncase/runtime/runtime_op_utility.h>
#include <nncase/runtime/runtime_tensor.h>
#include <nncase/runtime/simple_types.h>
#include <nncase/runtime/util.h>
#include <onnxruntime_cxx_api.h>

#include "kaldifst/csrc/text-normalizer.h"
#include "sherpa-onnx/csrc/lexicon.h"
#include "sherpa-onnx/csrc/wave-writer.h"
#include "sherpa-onnx/csrc/piper-phonemize-lexicon.h"
#include "sherpa-onnx/csrc/offline-tts-vits-model-meta-data.h"
#include "k230_init.h"
#include "mmz.h"

extern "C" void mmz_shim_release_all(void);
extern "C" int kd_mpi_sys_mmz_flush_cache(uint64_t phy_addr, void *virt_addr, uint32_t len);
extern "C" uint64_t mmz_cpu_to_phys(uint64_t v);

using namespace nncase::runtime;

class KpuModel {
 public:
  explicit KpuModel(const std::string &kmodel_path, const char *tag)
      : path_(kmodel_path) {
    std::ifstream ifs(kmodel_path, std::ios::binary);
    if (!ifs.is_open()) {
      fprintf(stderr, "[KPU:%s] Error: Failed to open %s\n", tag, kmodel_path.c_str());
      exit(1);
    }
    // Load with copy_buffer=true: the gnne program section must stay valid for
    // the whole model lifetime (stream-loaded sections were observed unmapped
    // at first gnne_enable -> SIGSEGV in the cstage staging memcpy).
    kmodel_buf_.assign(std::istreambuf_iterator<char>(ifs),
                       std::istreambuf_iterator<char>());
    interp_.load_model(gsl::span<const gsl::byte>(
                           reinterpret_cast<const gsl::byte *>(kmodel_buf_.data()),
                           (gsl::span<const gsl::byte>::size_type)kmodel_buf_.size()),
                       true)
        .expect("[KPU] Invalid kmodel");

    for (size_t i = 0; i < interp_.inputs_size(); ++i) {
      auto desc = interp_.input_desc(i);
      auto shape = interp_.input_shape(i);
      auto tensor = host_runtime_tensor::create(desc.datatype, shape, hrt::pool_shared)
                        .expect("[KPU] cannot create input tensor");
      interp_.input_tensor(i, tensor).expect("[KPU] cannot set input tensor");
      auto map = host_runtime_tensor::map(tensor, map_write).expect("[KPU] cannot map input tensor");
      input_tensors_.push_back(tensor);
      input_maps_.push_back(std::move(map));
      input_ptrs_.push_back(input_maps_.back().buffer().data());
      input_bytes_.push_back(input_maps_.back().buffer().size_bytes());
      fprintf(stderr, "[KPU:%s] input %zu type=%d bytes=%zu phys=0x%llx\n", tag, i,
              static_cast<int>(desc.datatype), input_bytes_.back(),
              static_cast<unsigned long long>(mmz_cpu_to_phys(reinterpret_cast<uint64_t>(input_ptrs_.back()))));
    }

    for (size_t i = 0; i < interp_.outputs_size(); ++i) {
      auto desc = interp_.output_desc(i);
      auto shape = interp_.output_shape(i);
      auto tensor = host_runtime_tensor::create(desc.datatype, shape, hrt::pool_shared)
                        .expect("[KPU] cannot create output tensor");
      interp_.output_tensor(i, tensor).expect("[KPU] cannot set output tensor");
      auto map = host_runtime_tensor::map(tensor, map_read).expect("[KPU] cannot map output tensor");
      output_tensors_.push_back(tensor);
      output_maps_.push_back(std::move(map));
      output_ptrs_.push_back(output_maps_.back().buffer().data());
      output_bytes_.push_back(output_maps_.back().buffer().size_bytes());
      fprintf(stderr, "[KPU:%s] output %zu type=%d bytes=%zu phys=0x%llx\n", tag, i,
              static_cast<int>(desc.datatype), output_bytes_.back(),
              static_cast<unsigned long long>(mmz_cpu_to_phys(reinterpret_cast<uint64_t>(output_ptrs_.back()))));
    }
  }

  size_t input_count() const { return input_ptrs_.size(); }
  size_t output_count() const { return output_ptrs_.size(); }
  const std::string &path() const { return path_; }

  size_t input_bytes(size_t i) const { return input_bytes_[i]; }
  size_t output_bytes(size_t i) const { return output_bytes_[i]; }

  // byte sizes derived from the kmodel IO descriptors
  void Run(const std::vector<const void *> &in, const std::vector<void *> &out) {
    // models whose unused inputs (e.g. vestigial sid) were compiled away may
    // expose fewer inputs than the caller feeds — bind by position, capped
    size_t ni = std::min(in.size(), input_ptrs_.size());
    for (size_t i = 0; i < ni; ++i) {
      memcpy(input_ptrs_[i], in[i], input_bytes_[i]);
      kd_mpi_sys_mmz_flush_cache(0, input_ptrs_[i], static_cast<uint32_t>(input_bytes_[i]));
      host_runtime_tensor::sync(input_tensors_[i], sync_write_back, true).expect("[KPU] input sync failed");
    }
    interp_.run().expect("[KPU] interp run failed");
    for (size_t i = 0; i < out.size(); ++i) {
      kd_mpi_sys_mmz_flush_cache(0, output_ptrs_[i], static_cast<uint32_t>(output_bytes_[i]));
      memcpy(out[i], output_ptrs_[i], output_bytes_[i]);
    }
  }

 private:
  std::string path_;
  std::vector<char> kmodel_buf_;  // copied model buffer; must outlive interp_
  interpreter interp_;
  std::vector<runtime_tensor> input_tensors_;
  std::vector<mapped_buffer> input_maps_;
  std::vector<void *> input_ptrs_;
  std::vector<size_t> input_bytes_;
  std::vector<runtime_tensor> output_tensors_;
  std::vector<mapped_buffer> output_maps_;
  std::vector<void *> output_ptrs_;
  std::vector<size_t> output_bytes_;
};

// One-shot KPU invocation with scoped model lifetime: load -> run -> free.
// 9 concurrently-resident interpreters (~130MB of pool) segfault inside
// gnne_enable (cstage staging), so multi-piece chains keep a single kmodel
// resident at a time.
static std::vector<float> KpuRunScoped(const std::string &path, const char *tag,
                                       const std::vector<const void *> &in) {
  KpuModel k(path, tag);
  std::vector<float> out(k.output_bytes(0) / sizeof(float));
  fprintf(stderr, "[chain] %s in0=%zu out0=%zu\n", path.c_str(), k.input_bytes(0),
          k.output_bytes(0));
  k.Run(in, {out.data()});
  return out;
}

// Scoped run with SIZE-MATCHED feeds: kmodel IO order is not discoverable at
// runtime (tensor_desc has no name), so bind z-sized, mask-sized and sid
// inputs by their byte sizes instead of assuming descriptor order.
static std::vector<float> KpuRunScopedFlow(const std::string &path, const char *tag,
                                           const std::vector<float> &in0,
                                           const std::vector<float> &m, int64_t sid0) {
  KpuModel k(path, tag);
  std::vector<const void *> feeds(k.input_count(), &sid0);
  for (size_t i = 0; i < k.input_count(); ++i) {
    if (k.input_bytes(i) == in0.size() * sizeof(float))
      feeds[i] = in0.data();
    else if (!m.empty() && k.input_bytes(i) == m.size() * sizeof(float))
      feeds[i] = m.data();
  }
  std::vector<float> out(k.output_bytes(0) / sizeof(float));
  fprintf(stderr, "[chain] %s in0=%zu out0=%zu\n", path.c_str(), k.input_bytes(0),
          k.output_bytes(0));
  k.Run(feeds, {out.data()});
  return out;
}

// (cur,sid) KPU piece-chain with an optional mid-chain CPU splice:
// group A pieces run on KPU from cur, then cpu_ses (if set) maps cur -> cur
// (f32, shape from the session's own input descriptor), then group B pieces
// run on KPU. The '|' in --melo-subgen separates the two KPU groups.
static std::vector<float> RunChainKpuCpuKpu(const std::vector<std::string> &gA,
                                            const std::vector<std::string> &gB,
                                            Ort::Session *cpu_ses,
                                            std::vector<float> cur, int64_t sid0) {
  for (size_t p = 0; p < gA.size(); ++p)
    cur = KpuRunScoped(gA[p], "meloPA", {cur.data(), &sid0});
  if (cpu_ses) {
    Ort::MemoryInfo mem = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);
    auto ish = cpu_ses->GetInputTypeInfo(0).GetTensorTypeAndShapeInfo().GetShape();
    int64_t shp[3] = {1, ish[1], ish[2]};
    int64_t one[1] = {1};
    const char *in_names[] = {"/dec/LeakyRelu_3_output_0", "sid"};
    const char *out_names[] = {"/dec/LeakyRelu_5_output_0"};
    std::vector<Ort::Value> iv;
    iv.push_back(Ort::Value::CreateTensor<float>(mem, (float *)cur.data(), cur.size(), shp, 3));
    iv.push_back(Ort::Value::CreateTensor<int64_t>(mem, &sid0, 1, one, 1));
    auto ov = cpu_ses->Run(Ort::RunOptions{nullptr}, in_names, iv.data(), 2, out_names, 1);
    size_t n = (size_t)ov[0].GetTensorTypeAndShapeInfo().GetElementCount();
    std::vector<float> out(n);
    memcpy(out.data(), ov[0].GetTensorData<float>(), n * sizeof(float));
    cur.swap(out);
  }
  for (size_t p = 0; p < gB.size(); ++p)
    cur = KpuRunScoped(gB[p], "meloPB", {cur.data(), &sid0});
  return cur;
}

// Resident-model variant of RunChainKpuCpuKpu (models stay loaded across
// sentences; loading all pieces per sentence costs ~15s of file IO each).
static std::vector<float> RunChainResident(const std::vector<KpuModel *> &gA,
                                           const std::vector<KpuModel *> &gB,
                                           Ort::Session *cpu_ses,
                                           std::vector<float> cur, int64_t sid0) {
  using clk = std::chrono::steady_clock;
  for (size_t p = 0; p < gA.size(); ++p) {
    auto t0 = clk::now();
    std::vector<float> nxt(gA[p]->output_bytes(0) / sizeof(float));
    gA[p]->Run({cur.data(), &sid0}, {nxt.data()});
    fprintf(stderr, "[chain-t] %s %.1f ms\n", gA[p]->path().c_str(),
            std::chrono::duration<double, std::milli>(clk::now() - t0).count());
    cur.swap(nxt);
  }
  if (cpu_ses) {
    auto t0 = clk::now();
    Ort::MemoryInfo mem = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);
    auto ish = cpu_ses->GetInputTypeInfo(0).GetTensorTypeAndShapeInfo().GetShape();
    int64_t shp[3] = {1, ish[1], ish[2]};
    int64_t one[1] = {1};
    const char *in_names[] = {"/dec/LeakyRelu_3_output_0", "sid"};
    const char *out_names[] = {"/dec/LeakyRelu_5_output_0"};
    std::vector<Ort::Value> iv;
    iv.push_back(Ort::Value::CreateTensor<float>(mem, (float *)cur.data(), cur.size(), shp, 3));
    iv.push_back(Ort::Value::CreateTensor<int64_t>(mem, &sid0, 1, one, 1));
    auto ov = cpu_ses->Run(Ort::RunOptions{nullptr}, in_names, iv.data(), 2, out_names, 1);
    size_t n = (size_t)ov[0].GetTensorTypeAndShapeInfo().GetElementCount();
    std::vector<float> out(n);
    memcpy(out.data(), ov[0].GetTensorData<float>(), n * sizeof(float));
    fprintf(stderr, "[chain-t] MIDCPU %.1f ms\n",
            std::chrono::duration<double, std::milli>(clk::now() - t0).count());
    cur.swap(out);
  }
  for (size_t p = 0; p < gB.size(); ++p) {
    auto t0 = clk::now();
    std::vector<float> nxt(gB[p]->output_bytes(0) / sizeof(float));
    gB[p]->Run({cur.data(), &sid0}, {nxt.data()});
    fprintf(stderr, "[chain-t] %s %.1f ms\n", gB[p]->path().c_str(),
            std::chrono::duration<double, std::milli>(clk::now() - t0).count());
    cur.swap(nxt);
  }
  return cur;
}

// parse "a,b,c|d,e" into [[a,b,c],[d,e]]
static std::vector<std::vector<std::string>> ParseSubgenGroups(const std::string &s) {
  std::vector<std::vector<std::string>> groups;
  std::vector<std::string> cur;
  std::string acc;
  auto flush_item = [&]() {
    if (!acc.empty()) cur.push_back(acc);
    acc.clear();
  };
  auto flush_group = [&]() {
    flush_item();
    if (!cur.empty()) groups.push_back(cur);
    cur.clear();
  };
  for (char c : s) {
    if (c == ',') flush_item();
    else if (c == '|') flush_group();
    else acc += c;
  }
  flush_group();
  return groups;
}

template <typename T>
static std::vector<T> ReadBinary(const std::string &path) {
  std::ifstream f(path, std::ios::binary | std::ios::ate);
  if (!f.is_open()) {
    fprintf(stderr, "Cannot open %s\n", path.c_str());
    return {};
  }
  size_t bytes = static_cast<size_t>(f.tellg());
  if (bytes % sizeof(T) != 0) {
    fprintf(stderr, "Invalid binary size: %s\n", path.c_str());
    return {};
  }
  std::vector<T> data(bytes / sizeof(T));
  f.seekg(0);
  f.read(reinterpret_cast<char *>(data.data()), bytes);
  return data;
}

static int RunProbe(const std::string &kmodel_path, const std::string &probe_dir) {
  k230_kpu_init();
  auto *kpu = new KpuModel(kmodel_path, "subgen");
  int failures = 0;

  for (int i = 0; i < 2; ++i) {
    const std::string stem = probe_dir + "/probe_" + std::to_string(i);
    int valid_frames = 0;
    int64_t expected_speaker = -1;
    std::ifstream meta(stem + ".txt");
    meta >> valid_frames >> expected_speaker;

    auto z = ReadBinary<float>(stem + "_z.bin");
    auto mask = ReadBinary<uint8_t>(stem + "_m.bin");
    auto speaker = ReadBinary<int64_t>(stem + "_s.bin");
    auto ref = ReadBinary<float>(stem + "_ref.bin");
    const size_t frames = kpu->input_bytes(0) / sizeof(float) / 96;
    if (z.size() != 96 * frames || mask.size() != frames ||
        speaker.size() != 1 || speaker[0] != expected_speaker ||
        ref.size() != static_cast<size_t>(valid_frames) * 256) {
      fprintf(stderr, "Probe %d input size mismatch\n", i);
      ++failures;
      continue;
    }

    std::vector<float> output(kpu->output_bytes(0) / sizeof(float));
    const clock_t cpu_start = std::clock();
    const auto wall_start = std::chrono::steady_clock::now();
    kpu->Run({z.data(), mask.data(), speaker.data()}, {output.data()});
    const auto wall_end = std::chrono::steady_clock::now();
    const clock_t cpu_end = std::clock();

    double dot = 0.0;
    double ref_norm = 0.0;
    double out_norm = 0.0;
    double squared_error = 0.0;
    double max_error = 0.0;
    size_t non_finite = 0;
    for (size_t j = 0; j < ref.size(); ++j) {
      const double a = ref[j];
      const double b = output[j];
      if (!std::isfinite(b)) ++non_finite;
      const double error = std::abs(a - b);
      dot += a * b;
      ref_norm += a * a;
      out_norm += b * b;
      squared_error += error * error;
      max_error = std::max(max_error, error);
    }
    const double cos = dot / (std::sqrt(ref_norm * out_norm) + 1e-30);
    const double rmse = std::sqrt(squared_error / ref.size());
    const double wall_ms = std::chrono::duration<double, std::milli>(wall_end - wall_start).count();
    const double cpu_ms = 1000.0 * static_cast<double>(cpu_end - cpu_start) / CLOCKS_PER_SEC;
    printf("PROBE %d sid=%ld frames=%d cos=%.8f rmse=%.8g max=%.8g nonfinite=%zu wall_ms=%.3f cpu_ms=%.3f\n",
           i, static_cast<long>(speaker[0]), valid_frames, cos, rmse, max_error,
           non_finite, wall_ms, cpu_ms);

    std::ofstream out(stem + "_kpu.bin", std::ios::binary);
    out.write(reinterpret_cast<const char *>(output.data()), output.size() * sizeof(float));
    if (cos < 0.98 || non_finite != 0) ++failures;
  }

  printf("PROBE_RESULT failures=%d\n", failures);
  return failures == 0 ? 0 : 2;
}

int main(int argc, char *argv[]) {
  setvbuf(stdout, NULL, _IONBF, 0);
  setvbuf(stderr, NULL, _IONBF, 0);
  // production gnne defaults: quiet + local-buffer preseed. chatd doesn't set
  // these, and without them every layer call spams the log (913k lines/30
  // utterances measured) and the non-preseed alloc path is slower. 0 flag =
  // never overrides an externally provided value.
  setenv("GNNE_QUIET", "1", 0);
  setenv("KPU_LOCAL", "1", 0);
  if (argc >= 2 && std::string(argv[1]) == "--probe") {
    if (argc != 4) {
      fprintf(stderr, "Usage: %s --probe <subgen.kmodel> <probe_dir>\n", argv[0]);
      return 1;
    }
    const int rc = RunProbe(argv[2], argv[3]);
    mmz_shim_release_all();
    _exit(rc);
  }
  bool daemon_mode = false;
  if (argc >= 2 && std::string(argv[1]) == "--probe-melo") {
    // subgen kmodel numerical probe: <dir>/z.f32 [192*256], m.f32 [256] ->
    // <dir>/y.f32 (compare against the x86 f32 reference on PC!)
    if (argc != 4) {
      fprintf(stderr, "Usage: %s --probe-melo <subgen.kmodel> <dir>\n", argv[0]);
      return 1;
    }
    k230_kpu_init();
    KpuModel ks(argv[2], "meloP");
    auto z = ReadBinary<float>(std::string(argv[3]) + "/z.f32");
    auto m = ReadBinary<float>(std::string(argv[3]) + "/m.f32");
    if (z.size() != 192 * 256 || m.size() != 256) {
      fprintf(stderr, "probe-melo size mismatch: z=%zu m=%zu\n", z.size(), m.size());
      return 1;
    }
    int64_t sid0 = 0;
    std::vector<float> y(ks.output_bytes(0) / sizeof(float));
    {
      // size-matched feeds: bind z/m/sid by byte size, not descriptor order
      std::vector<const void *> feeds(ks.input_count(), &sid0);
      for (size_t i = 0; i < ks.input_count(); ++i) {
        if (ks.input_bytes(i) == z.size() * sizeof(float)) feeds[i] = z.data();
        else if (ks.input_bytes(i) == m.size() * sizeof(float)) feeds[i] = m.data();
      }
      ks.Run(feeds, {y.data()});
    }
    FILE *f = fopen((std::string(argv[3]) + "/y.f32").c_str(), "wb");
    if (!f) { perror("fopen"); return 1; }
    fwrite(y.data(), 4, y.size(), f);
    fclose(f);
    printf("[probe-melo] wrote y.f32 (%zu floats)\n", y.size());
    mmz_shim_release_all();
    _exit(0);
  }
  if (argc >= 2 && std::string(argv[1]) == "--daemon") {
    daemon_mode = true;
  } else if (argc >= 2 && std::string(argv[1]) == "--probe-est") {
    // est128 kmodel probe: reads <dir>/mm.f32 [128*80], mask.f32 [128],
    // mu.f32 [80*128]; runs 3 Euler steps (t=0,1/3,2/3, z = randn*ns from
    // MATCHA_NS, 0 default) and writes <dir>/mel.f32 [80*128] (denormalized).
    if (argc != 4) {
      fprintf(stderr, "Usage: %s --probe-est <est.kmodel> <dir>\n", argv[0]);
      return 1;
    }
    k230_kpu_init();
    // optional: reproduce the runner context (ORT session created, then KpuModel,
    // then an ORT inference, then KPU runs) to test ORT->KPU interference
    std::unique_ptr<Ort::Session> pw_aco;
    if (getenv("PROBE_ORT_WARM")) {
      Ort::Env menv(ORT_LOGGING_LEVEL_WARNING, "probew");
      Ort::SessionOptions mso;
      mso.SetIntraOpNumThreads(1);
      mso.SetInterOpNumThreads(1);
      pw_aco = std::make_unique<Ort::Session>(menv, getenv("PROBE_ORT_WARM"), mso);
    }
    KpuModel ke(argv[2], "est");
    if (pw_aco) {
      const int64_t shp[2] = {1, 21};
      std::vector<int64_t> ids(21, 1);
      int64_t xl = 21;
      float nsf = 0.f, lsf = 1.f;
      int64_t one[1] = {1};
      const char *pin[] = {"x", "x_length", "noise_scale", "length_scale"};
      auto memi = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);
      std::vector<Ort::Value> pv;
      pv.push_back(Ort::Value::CreateTensor<int64_t>(memi, ids.data(), ids.size(), shp, 2));
      pv.push_back(Ort::Value::CreateTensor<int64_t>(memi, &xl, 1, one, 1));
      pv.push_back(Ort::Value::CreateTensor<float>(memi, &nsf, 1, one, 1));
      pv.push_back(Ort::Value::CreateTensor<float>(memi, &lsf, 1, one, 1));
      const char *pout[] = {"/MatMul_output_0"};
      auto r = pw_aco->Run(Ort::RunOptions{nullptr}, pin, pv.data(), 4, pout, 1);
      printf("[probe-est] ORT warmup done\n");
    }
    auto mm = ReadBinary<float>(std::string(argv[3]) + "/mm.f32");
    auto mk = ReadBinary<float>(std::string(argv[3]) + "/mask.f32");
    auto mu = ReadBinary<float>(std::string(argv[3]) + "/mu.f32");
    if (mm.size() != 128 * 80 || mk.size() != 128 || mu.size() != 80 * 128) {
      fprintf(stderr, "probe-est size mismatch: mm=%zu mask=%zu mu=%zu\n",
              mm.size(), mk.size(), mu.size());
      return 1;
    }
    // mm is [128,80] t-major; kmodel wants x/mu [80,128] ch-major
    std::vector<float> x(80 * 128), muT(80 * 128), v(80 * 128);
    for (int t = 0; t < 128; ++t)
      for (int ch = 0; ch < 80; ++ch) muT[ch * 128 + t] = mm[t * 80 + ch];
    float ns = getenv("MATCHA_NS") ? (float)atof(getenv("MATCHA_NS")) : 0.0f;
    std::mt19937 rng(42);
    std::normal_distribution<float> g(0.f, ns);
    for (auto &e : x) e = g(rng);
    for (int k = 0; k < 3; ++k) {
      float t = k / 3.0f;
      ke.Run({x.data(), mk.data(), muT.data(), &t}, {v.data()});
      for (int i = 0; i < 80 * 128; ++i) x[i] += v[i] / 3.0f;
    }
    for (int i = 0; i < 80 * 128; ++i) x[i] = x[i] * 2.7628188f - 5.9870973f;
    FILE *f = fopen((std::string(argv[3]) + "/mel.f32").c_str(), "wb");
    if (!f) { perror("fopen"); return 1; }
    fwrite(x.data(), 4, x.size(), f);
    fclose(f);
    printf("[probe-est] wrote mel.f32\n");
    mmz_shim_release_all();
    _exit(0);
  } else if (argc < 2) {
    fprintf(stderr,
            "Usage: %s <text> [options]\n"
            "       %s --daemon [options]   (resident worker, reads stdin)\n"
            "Options:\n"
            "  --subgen-kmodel=<path> (default: /mnt/data/matcha/subgen.kmodel)\n"
            "  --enc-onnx=<path>      (default: /mnt/data/matcha/enc_dp.onnx)\n"
            "  --enc-only-onnx=<path> (default: /mnt/data/matcha/enc.onnx; mu+logs only,\n"
            "                         used for the CPU-enc + --dp-kmodel combo)\n"
            "  --dp-kmodel=<path>     (duration predictor on KPU; empty = ORT dp)\n"
            "  --lexicon=<path>       (default: /mnt/data/matcha/lexicon.txt)\n"
            "  --tokens=<path>        (default: /mnt/data/matcha/tokens.txt)\n"
            "  --rule-fsts=<path>     (default: /mnt/data/matcha/phone.fst,/mnt/data/matcha/date.fst,/mnt/data/matcha/number.fst)\n"
            "  --sid=<int>            (default: 10)\n"
            "  --speed=<float>        (default: 1.0)\n"
            "  --output=<path>        (default: /mnt/data/matcha/tts_kpu_out.wav)\n"
            "  --bench                (per-stage rerun timings on first sentence)\n"
            "  --daemon               resident worker: models load ONCE, one\n"
            "                         utterance per stdin line\n"
            "Daemon line protocol: <wav_path>\t<sid>\t<speed>\t<text>\n"
            "  (text is the LAST field, may contain tabs; // or empty lines skipped)\n"
            "  stdout: READY after init; DONE <wav> <audio_sec> <rtf> per utterance;\n"
            "          ERROR <reason> on failure. QUIT or EOF stops the worker.\n",
            argv[0], argv[0]);
    return 1;
  }

  std::string text = daemon_mode ? "" : argv[1];
  std::string subgen_kmodel = "/mnt/data/matcha/subgen.kmodel";
  std::string enc_onnx = "/mnt/data/matcha/enc_dp.onnx";
  std::string enc_only_onnx = "/mnt/data/matcha/enc.onnx";
  std::string lexicon_path = "/mnt/data/matcha/lexicon.txt";
  std::string tokens_path = "/mnt/data/matcha/tokens.txt";
  std::string rule_fsts = "/mnt/data/matcha/phone.fst,/mnt/data/matcha/date.fst,/mnt/data/matcha/number.fst";
  int64_t sid = 10;
  float speed = 1.0f;
  bool bench = false;
  bool use_shl = false;
  std::string enc_kmodel;  // empty = text_encoder stays on CPU via enc_dp.onnx
  std::string dp_kmodel;   // empty = duration predictor via ORT
  std::string dp_onnx = "/mnt/data/matcha/dp.onnx";
  std::string out_wav = "/mnt/data/matcha/tts_kpu_out.wav";
  bool compare_enc = false;   // debug: run CPU enc alongside KPU enc, print cos
  std::string dump_logw;      // debug: dump kmodel logw + ORT logw per sentence
  float nsd_override = -1.0f; // debug: override noise_scale_dur (0 = deterministic)
  std::string probe_dpx;      // debug: raw dp_x.bin + tok.bin + tl + spk probe
  std::string probe_matcha;   // debug: matcha probe (tokens.bin)
  std::string matcha_say;     // matcha: synthesize text to --output wav
  std::string matcha_acoustic;
  std::string matcha_vocoder;
  std::string matcha_kmodel;
  std::string matcha_a_kmodel;
  std::string matcha_c_kmodel;
  std::string matcha_c_onnx;
  std::string matcha_est_kmodel;  // single-step decoder (estimator) on KPU
  std::string probe_est;          // dir with mm.f32/mask.f32/mu.f32 -> mel.f32
  std::string melo_say;           // melo: text -> pregen CPU + subgen KPU
  std::string piper_say;          // piper: text -> espeak phonemize + KPU chain
  std::string piper_dir;          // dir with piper kmodels/tokens/espeak data
  std::string piper_tokens_file;  // optional: precomputed ids (.i64) to bypass
                                  // espeak frontend (eval/debug)
  std::string melo_pregen;
  std::string melo_flow;          // f32 CPU onnx: z_pre+mask+sid -> z_hat
                                  // (attention-heavy flow stays on CPU; the
                                  // K230 GNNE mis-executes its dynamic MatMuls)
  std::string melo_midcpu;        // f32 CPU onnx spliced into the KPU chain at
                                  // the '|' boundary of --melo-subgen
                                  // (dq4+dq5's ups/resblocks also mis-execute)
  std::string melo_subgen;
  std::string melo_lexicon;
  std::string melo_tokens;
  std::string probe_chain;        // dir with z.f32/m.f32 -> run whole subgen
                                  // chain (no pregen) -> chain.f32 (numerical
                                  // gate vs x86 ONNX reference on PC)
  std::string matcha_lexicon;
  std::string matcha_tokens;

  // argv[1] is normally <text>; only the --probe-chain mode may live there
  // (the loop below starts at i=2).
  if (argc >= 2 && std::string(argv[1]).rfind("--probe-chain=", 0) == 0)
    probe_chain = std::string(argv[1]).substr(14);

  for (int i = 2; i < argc; ++i) {
    std::string arg = argv[i];
    if (arg.rfind("--subgen-kmodel=", 0) == 0) subgen_kmodel = arg.substr(16);
    else if (arg.rfind("--enc-onnx=", 0) == 0) enc_onnx = arg.substr(11);
    else if (arg.rfind("--enc-only-onnx=", 0) == 0) enc_only_onnx = arg.substr(16);
    else if (arg.rfind("--lexicon=", 0) == 0) lexicon_path = arg.substr(10);
    else if (arg.rfind("--tokens=", 0) == 0) tokens_path = arg.substr(9);
    else if (arg.rfind("--rule-fsts=", 0) == 0) rule_fsts = arg.substr(12);
    else if (arg.rfind("--sid=", 0) == 0) sid = std::stoll(arg.substr(6));
    else if (arg.rfind("--speed=", 0) == 0) speed = std::stof(arg.substr(8));
    else if (arg == "--bench") bench = true;
    else if (arg == "--daemon") daemon_mode = true;
    else if (arg == "--shl") use_shl = true;
    else if (arg.rfind("--enc-kmodel=", 0) == 0) enc_kmodel = arg.substr(13);
    else if (arg.rfind("--dp-kmodel=", 0) == 0) dp_kmodel = arg.substr(12);
    else if (arg.rfind("--dp-onnx=", 0) == 0) dp_onnx = arg.substr(10);
    else if (arg == "--compare-enc") compare_enc = true;
    else if (arg.rfind("--dump-logw=", 0) == 0) dump_logw = arg.substr(12);
    else if (arg.rfind("--nsd=", 0) == 0) nsd_override = std::stof(arg.substr(6));
    else if (arg.rfind("--probe-dpx=", 0) == 0) probe_dpx = arg.substr(12);
    else if (arg.rfind("--probe-matcha=", 0) == 0) probe_matcha = arg.substr(15);
    else if (arg.rfind("--matcha-say=", 0) == 0) matcha_say = arg.substr(13);
    else if (arg.rfind("--matcha-acoustic=", 0) == 0) matcha_acoustic = arg.substr(18);
    else if (arg.rfind("--matcha-vocoder=", 0) == 0) matcha_vocoder = arg.substr(17);
    else if (arg.rfind("--matcha-kmodel=", 0) == 0) matcha_kmodel = arg.substr(16);
    else if (arg.rfind("--matcha-a-kmodel=", 0) == 0) matcha_a_kmodel = arg.substr(18);
    else if (arg.rfind("--matcha-c-kmodel=", 0) == 0) matcha_c_kmodel = arg.substr(18);
    else if (arg.rfind("--matcha-c-onnx=", 0) == 0) matcha_c_onnx = arg.substr(16);
    else if (arg.rfind("--matcha-est-kmodel=", 0) == 0) matcha_est_kmodel = arg.substr(20);
    else if (arg.rfind("--probe-est=", 0) == 0) probe_est = arg.substr(12);
    else if (arg.rfind("--melo-say=", 0) == 0) melo_say = arg.substr(11);
    else if (arg.rfind("--melo-flow=", 0) == 0) melo_flow = arg.substr(12);
    else if (arg.rfind("--melo-midcpu=", 0) == 0) melo_midcpu = arg.substr(14);
    else if (arg.rfind("--probe-chain=", 0) == 0) probe_chain = arg.substr(14);
    else if (arg.rfind("--melo-pregen=", 0) == 0) melo_pregen = arg.substr(14);
    else if (arg.rfind("--melo-subgen=", 0) == 0) melo_subgen = arg.substr(14);
    else if (arg.rfind("--melo-lexicon=", 0) == 0) melo_lexicon = arg.substr(15);
    else if (arg.rfind("--melo-tokens=", 0) == 0) melo_tokens = arg.substr(14);
    else if (arg.rfind("--matcha-lexicon=", 0) == 0) matcha_lexicon = arg.substr(17);
    else if (arg.rfind("--matcha-tokens=", 0) == 0) matcha_tokens = arg.substr(16);
    else if (arg.rfind("--piper-say=", 0) == 0) piper_say = arg.substr(12);
    else if (arg.rfind("--piper-dir=", 0) == 0) piper_dir = arg.substr(12);
    else if (arg.rfind("--piper-tokens-file=", 0) == 0) piper_tokens_file = arg.substr(20);
    else if (arg.rfind("--output=", 0) == 0) out_wav = arg.substr(9);
  }

  printf("=== KPU VITS TTS Runner ===\n");

  // ==== piper end-to-end: text -> espeak phonemize (sherpa C++) -> enc KPU
  // ==== -> dp mid block (CPU ORT, dynamic) -> pad to F=128 -> flow 4 KPU
  // ==== pieces (resident, dual-output chain) -> dec 20 KPU pieces (resident)
  // ==== -> wav @22050. Eval build: eps noise loaded from fixed files.
  if (!piper_say.empty() || (daemon_mode && !piper_dir.empty())) {
    if (piper_dir.empty()) {
      fprintf(stderr, "--piper-say/--daemon-piper needs --piper-dir=<dir>\n");
      return 1;
    }
    const std::string PD = piper_dir.back() == '/' ? piper_dir : piper_dir + "/";
    const int FT = 128;                 // flow/dec frame bucket
    const int ENC_T = 79;               // enc token bucket (static kmodel)
    k230_kpu_init();
    using clk2 = std::chrono::steady_clock;
    auto cpu_ms_p = []() {
      struct rusage ru;
      getrusage(RUSAGE_SELF, &ru);
      return (ru.ru_utime.tv_sec + ru.ru_stime.tv_sec) * 1000.0 +
             (ru.ru_utime.tv_usec + ru.ru_stime.tv_usec) / 1000.0;
    };
    double cp0 = cpu_ms_p();
    auto t_all0 = clk2::now();

    // --- frontend (skipped entirely on the tokens-file eval path: espeak
    // init is the prime heap-corruption suspect on riscv) ---
    sherpa_onnx::OfflineTtsVitsModelMetaData meta;
    meta.is_piper = true;
    meta.sample_rate = 22050;
    std::unique_ptr<sherpa_onnx::PiperPhonemizeLexicon> lexicon;
    if (piper_tokens_file.empty())
      lexicon = std::make_unique<sherpa_onnx::PiperPhonemizeLexicon>(
          PD + "tokens.txt", PD + "espeak-ng-data", meta);
    fprintf(stderr, "[piper] lexicon ready\n");

    // --- resident models: ALL of them (enc + flow4 + dec20), loaded once in
    // a pure-alloc phase like the resdec probe. Interleaved scoped load/free
    // (enc/flow scoped per sentence) corrupted MMZ segment reuse -> segfault
    // at the first dec gnne_enable. espeak (heap corruptor suspect) stays
    // skipped on the tokens-file path.
    KpuModel enc(PD + "piper_enc" +
                     std::string(getenv("PIPER_ENC_SFX") ? getenv("PIPER_ENC_SFX") : "") +
                     ".kmodel",
                 "pEnc");
    const char *fsfxe = getenv("PIPER_FLOW_SFX");
    std::string fsfx = fsfxe ? fsfxe : "";
    std::vector<std::unique_ptr<KpuModel>> flow;
    for (int j = 0; j < 4; ++j)
      flow.push_back(std::make_unique<KpuModel>(
          PD + "piper_flow_s" + std::to_string(j) + fsfx + ".kmodel", "pFlow"));
    static const int CT_IDXS[] = {1, 7, 13};
    auto is_ct = [&](int i) {
      for (int c : CT_IDXS)
        if (c == i) return true;
      return false;
    };
    std::vector<std::unique_ptr<KpuModel>> dec;
    bool nodec = getenv("PIPER_NODEC") != nullptr;  // bisect: skip dec load
    if (!nodec) {
      // PIPER_DEC_SFX: load e.g. p0_i8.kmodel for int8-quantized A/B tests
      const char *sfxe = getenv("PIPER_DEC_SFX");
      std::string sfx = sfxe ? sfxe : "";
      static const char *DEC_NAMES[] = {
          "p0", "ct0", "p1", "p2", "p3", "p4", "p5", "ct1", "p6", "p7", "p8",
          "p9", "p10", "ct2", "p11", "p12", "p13", "p14", "p15", "p16"};
      for (const char *n : DEC_NAMES)
        dec.push_back(
            std::make_unique<KpuModel>(PD + n + sfx + ".kmodel", "pDec"));
    }
    fprintf(stderr, "[piper] models resident: enc+flow4%s\n",
            nodec ? " (dec skipped)" : "+dec20");
    double cp1 = cpu_ms_p();
    auto t_ld1 = clk2::now();

    // mid dp block on CPU ORT (dynamic frame count)
    bool noort = getenv("PIPER_NOORT") != nullptr;  // bisect: skip ORT session
    bool dpkpu = getenv("PIPER_DP_KPU") != nullptr;
    Ort::Env penv(ORT_LOGGING_LEVEL_ERROR, "piper-pipe");
    Ort::SessionOptions pso;
    pso.SetIntraOpNumThreads(1);
    // ORT 1.14 graph optimization mis-handles the internal RandomNormalLike
    // ops (downstream Mul sees "Missing Input") — run the raw graph.
    pso.SetGraphOptimizationLevel(GraphOptimizationLevel::ORT_DISABLE_ALL);
    std::unique_ptr<Ort::Session> midp;
    // DP_KPU=1 时 mid 不参与运算（dp 走 KPU 四片 + att 三片，见下），FP32
    // 2.45MB session 纯驻留浪费 —— 内存审计实测后跳过加载；PIPER_MID_ALWAYS 兜底
    if ((!noort || getenv("PIPER_SKIP") == nullptr) &&
        !dpkpu && getenv("PIPER_MID_ALWAYS") == nullptr)
      midp = std::make_unique<Ort::Session>(
          penv, (PD + "piper_mid_a.onnx").c_str(), pso);
    Ort::Session *mid = midp.get();
    Ort::MemoryInfo pmem = Ort::MemoryInfo::CreateCpu(
        OrtAllocatorType::OrtArenaAllocator, OrtMemTypeDefault);

    // PIPER_DP_KPU=1: dp conv backbone on 4 float KPU kmodels (post/f7/f5/f3),
    // the TransformerCoupling attention gymnastics stay on 3 small ORT graphs.
    // Execution per sentence (interleaved):
    //   post(o2,o1) -> f7(x0=zf[0],post,o1) -> att7(h29,zf,o1) -> c7
    //   f5(x0=(c7*mask)[1],post,o1) -> att5(h29,c7,o1) -> c5
    //   f3(x0=c5[1],post,o1)        -> att3(h29,c5,o1) -> dur
    std::unique_ptr<KpuModel> dp_post_m, dp_f7_m, dp_f5_m, dp_f3_m;
    std::unique_ptr<Ort::Session> att7s, att5s, att3s;
    if (dpkpu) {
      dp_post_m = std::make_unique<KpuModel>(PD + "piper_dp_post.kmodel", "dpP");
      dp_f7_m = std::make_unique<KpuModel>(PD + "piper_dp_f7.kmodel", "dp7");
      dp_f5_m = std::make_unique<KpuModel>(PD + "piper_dp_f5.kmodel", "dp5");
      dp_f3_m = std::make_unique<KpuModel>(PD + "piper_dp_f3.kmodel", "dp3");
      att7s = std::make_unique<Ort::Session>(penv, (PD + "piper_dp_att7.onnx").c_str(), pso);
      att5s = std::make_unique<Ort::Session>(penv, (PD + "piper_dp_att5.onnx").c_str(), pso);
      att3s = std::make_unique<Ort::Session>(penv, (PD + "piper_dp_att3.onnx").c_str(), pso);
    }
    std::vector<float> dp_post(192 * 79), dp_h29(29 * 79), dp_c7(2 * 79),
        dp_c5(2 * 79), dp_dur(79), dp_zf(2 * 79), dp_x0(79), dp_masked(2 * 79);
    std::vector<Ort::Value> dp_fin;
    auto dp_run1 = [&](Ort::Session &ses, float *const *ins,
                       const int64_t *shps, const size_t *lens, size_t nin,
                       float *out) {
      auto t_dp0 = clk2::now();
      dp_fin.clear();
      for (size_t i = 0; i < nin; ++i)
        dp_fin.push_back(Ort::Value::CreateTensor<float>(
            pmem, ins[i], lens[i], shps + i * 3, 3));
      Ort::AllocatorWithDefaultOptions al;
      std::vector<Ort::AllocatedStringPtr> keep;
      std::vector<const char *> inames, onames;
      for (size_t i = 0; i < ses.GetInputCount(); ++i) {
        keep.push_back(ses.GetInputNameAllocated(i, al));
        inames.push_back(keep.back().get());
      }
      for (size_t i = 0; i < ses.GetOutputCount(); ++i) {
        keep.push_back(ses.GetOutputNameAllocated(i, al));
        onames.push_back(keep.back().get());
      }
      auto ov = ses.Run(Ort::RunOptions{nullptr}, inames.data(), dp_fin.data(),
                        nin, onames.data(), 1);
      memcpy(out, ov[0].GetTensorMutableData<float>(),
             ov[0].GetTensorTypeAndShapeInfo().GetElementCount() * 4);
      if (getenv("PIPER_DP_DBG"))
        fprintf(stderr, "[dp] att run %.1f ms\n",
                std::chrono::duration<double, std::milli>(clk2::now() - t_dp0)
                    .count());
    };

    std::vector<float> zp128(192 * FT, 0.f), mask128(FT, 0.f);
    std::vector<float> dec_in(192 * FT), hT(192 * FT), hT2(192 * FT);
    std::vector<float> tmp(4 * 1024 * 1024);
    std::vector<float> sk0(128 * 1024), sk1(64 * 8192), sk2(32 * 32768);
    std::vector<int64_t> ids79(ENC_T, 0);

    // mid dp block v2: noise is INTERNAL (RandomNormalLike) — inputs are
    // BND(5) + scales only; z noise is sampled at the expanded rate inside.
    std::vector<float> scales{0.667f, 1.0f, 0.8f};
    auto sks_pick = [](std::vector<float> &a, std::vector<float> &b,
                       std::vector<float> &c, int i) -> std::vector<float> & {
      return i <= 0 ? a : (i == 1 ? b : c);
    };

    // ==== per-utterance loop: --daemon reads <wav>\t<sid>\t<speed>\t<text>
    // lines with models resident; single-shot runs exactly once. The RNGs
    // are re-created per utterance == old per-process determinism.
    if (daemon_mode) { printf("READY\n"); fflush(stdout); }
    int piper_utt = 0;
    std::string say = piper_say;
    std::mt19937 rngdp(12345);
    std::mt19937 rngmid(12345);
    for (;;) {
      if (daemon_mode) {
        std::string dline;
        if (!std::getline(std::cin, dline)) break;
        if (dline == "QUIT") break;
        if (dline.empty() || dline[0] == '#') continue;
        const size_t q1 = dline.find('\t');
        const size_t q2 =
            (q1 == std::string::npos) ? std::string::npos : dline.find('\t', q1 + 1);
        const size_t q3 =
            (q2 == std::string::npos) ? std::string::npos : dline.find('\t', q2 + 1);
        if (q3 == std::string::npos) {
          printf("ERROR protocol: expected <wav>\t<sid>\t<speed>\t<text>\n");
          fflush(stdout);
          continue;
        }
        const std::string wav_req = dline.substr(0, q1);
        const std::string spd_req = dline.substr(q2 + 1, q3 - q2 - 1);
        say = dline.substr(q3 + 1);
        if (!wav_req.empty()) out_wav = wav_req;
        try {
          float spd = std::stof(spd_req);
          scales[1] = (spd > 0) ? (1.0f / spd) : 1.0f;
        } catch (...) { scales[1] = 1.0f; }
        cp0 = cpu_ms_p();
        t_all0 = clk2::now();
      } else if (piper_utt > 0) {
        break;
      }
      ++piper_utt;
    std::vector<std::vector<int64_t>> sents;
    auto t_fe0 = clk2::now();
    if (!piper_tokens_file.empty()) {
      // raw int64 ids dumped by the x86 prep (single sentence, eval path)
      FILE *fi = fopen(piper_tokens_file.c_str(), "rb");
      if (!fi) { perror("tokens file"); return 1; }
      std::vector<int64_t> ids;
      int64_t v64;
      while (fread(&v64, 8, 1, fi) == 1) ids.push_back(v64);
      fclose(fi);
      sents.push_back(std::move(ids));
    } else {
      // Pre-split at punctuation (keeping it inside each chunk so espeak
      // emits the pause phone): sherpa only splits at 。！？, and any chunk
      // longer than the 79-token enc bucket would be silently truncated.
      auto utf8_len = [](unsigned char c) {
        if (c < 0x80) return 1;
        if ((c >> 5) == 0x6) return 2;
        if ((c >> 4) == 0xE) return 3;
        return 4;
      };
      static const char *PUNCT3[] = {"，", "。", "！", "？", "；", "、", "："};
      std::vector<std::string> pieces;
      std::string cur;
      for (size_t i = 0; i < say.size();) {
        int cl = utf8_len((unsigned char)say[i]);
        cur.append(say, i, cl);
        bool punct = cl == 1 && (say[i] == ',' || say[i] == '.' ||
                                 say[i] == '!' || say[i] == '?' ||
                                 say[i] == ';' || say[i] == ':');
        if (cl == 3)
          for (const char *p : PUNCT3)
            if (say.compare(i, 3, p) == 0) punct = true;
        i += cl;
        if (punct && !cur.empty()) {
          pieces.push_back(cur);
          cur.clear();
        }
      }
      if (!cur.empty()) pieces.push_back(cur);
      for (auto &pc : pieces) {
        auto toks = lexicon->ConvertTextToTokenIds(pc, "cmn");
        for (auto &t : toks) sents.emplace_back(t.tokens);
      }
      // --- 音素级切块：enc/dec 是静态 shape 桶（79 音素 / 128 帧=32768 采样），
      // 超长输入会被静默截断（"吃词"）。社区 piper 无此限制（动态 shape），
      // 这里按 ≤38 音素/块 切分后逐块走管线、音频自然拼接——调用方只管给句子
      {
        const size_t MAXT = 38;
        std::vector<std::vector<int64_t>> chunked;
        for (auto &t : sents) {
          if (t.size() <= MAXT) { chunked.push_back(t); continue; }
          for (size_t off = 0; off < t.size(); off += MAXT) {
            size_t end = std::min(off + MAXT, t.size());
            chunked.emplace_back(t.begin() + off, t.begin() + end);
          }
        }
        if (chunked.size() != sents.size()) {
          fprintf(stderr, "[piper] chunked %zu pieces -> %zu (MAXT=%zu)\n",
                  sents.size(), chunked.size(), MAXT);
          sents = std::move(chunked);
        }
      }
      if (getenv("PIPER_DUMP_IDS")) {
        for (size_t si = 0; si < sents.size(); ++si) {
          printf("[piper] sent %zu tokens(%zu):", si, sents[si].size());
          for (auto v : sents[si]) printf(" %lld", (long long)v);
          printf("\n");
        }
        FILE *fi2 = fopen((PD + "board_ids.f32").c_str(), "wb");
        if (fi2) {
          fwrite(sents[0].data(), 8, sents[0].size(), fi2);
          fclose(fi2);
        }
      }
    }
    auto t_fe1 = clk2::now();
    double t_front =
        std::chrono::duration<double, std::milli>(t_fe1 - t_fe0).count();
    printf("[piper] sentences: %zu, phonemize %.0f ms\n", sents.size(), t_front);
    if (sents.empty()) {
      if (daemon_mode) { printf("ERROR no sentences\n"); fflush(stdout); continue; }
      fprintf(stderr, "no sentences\n");
      return 1;
    }
    std::vector<float> audio_all;
    auto t_all1 = clk2::now();
    double t_synth = 0;
    for (auto &ids : sents) {
      // 1. enc (pad ids to ENC_T; input_lengths = padded count — the mid
      // block must see ONE consistent length; pad tail gets masked/capped)
      std::fill(ids79.begin(), ids79.end(), (int64_t)0);
      int64_t nreal = (int64_t)std::min(ids.size(), (size_t)ENC_T);
      for (int64_t i = 0; i < nreal; ++i) ids79[i] = ids[i];
      int64_t lens = nreal;  // real count: pad tokens must be MASKED (y_mask=0)
      // enc v3: outputs [H(1,384,79), Cast_1(x_mask), Mul_2, Unsqueeze].
      // The nncase Split op zeroes its second output, so we keep the
      // PRE-SPLIT H and slice m/logs on the host (channel-major contiguous:
      // m = h[0:15168], logs = h[15168:30336]).
      std::vector<float> o0(30336 + 16), o1(30336 + 16), o2(30336 + 16),
          o3(30336 + 16);
      // diagnostic bisect flags (eval build):
      // PIPER_SKIP=e  — skip enc+mid, read padded zp128/mask128 from PD files
      // PIPER_SKIP=m  — run enc, skip mid (z from files)
      // PIPER_FLOW_FRESH — after the resident chain, reload the 4 flow pieces
      //                    fresh and rerun (dump fsfresh.f32)
      const char *skipenv = getenv("PIPER_SKIP");
      std::string skips = skipenv ? skipenv : "";
      bool skip_all = skips.find('e') != std::string::npos;
      bool skip_mid = skips.find('m') != std::string::npos;
      bool fresh_flow = getenv("PIPER_FLOW_FRESH") != nullptr;
      const float *zp = nullptr;
      const float *mk = nullptr;
      int64_t Ly = 0;
      std::vector<float> zf, mf;
      std::vector<Ort::Value> mo;
      clk2::time_point ta = clk2::now(), tb = ta;
      double ca0 = cpu_ms_p(), ca1 = ca0, cb0 = ca0, cb1 = ca0,
             cc0 = ca0, cc1 = ca0, cd0 = ca0, cd1 = ca0;
      if (!skip_all) {
      ta = clk2::now();
      ca0 = cpu_ms_p();
      enc.Run({ids79.data(), &lens}, {o0.data(), o1.data(), o2.data(),
                                      o3.data()});
      fprintf(stderr, "[piper] enc done\n");
      // eval: dump enc outputs to identify nncase's output ordering
      if (getenv("PIPER_DUMP_ENC")) {
        enc.Run({ids79.data(), &lens}, {o0.data(), o1.data(), o2.data(),
                                        o3.data()});
        const char *on[] = {"o0", "o1", "o2", "o3", "o4"};
        std::vector<float> *ov[] = {&o0, &o1, &o2, &o3};
        for (int q = 0; q < 4; ++q) {
          FILE *fq = fopen((PD + "enc_" + on[q] + ".f32").c_str(), "wb");
          if (fq) {
            fwrite(ov[q]->data(), 4, ov[q]->size(), fq);
            fclose(fq);
          }
        }
        fprintf(stderr, "[piper] enc dumped\n");
      }
      tb = clk2::now();
      ca1 = cpu_ms_p();
      cb0 = cpu_ms_p();
      }

      // 2. mid on CPU, split in two (x86 split: dp=3.5ms, expansion=24ms of
      // dynamic-shape gymnastics -> move expansion to plain C loops here):
      //   mid_a.onnx: BND(5)+scales+dp_noise[1,2,79] -> masked durations [1,1,79]
      //   host C++:   dur_t = ceil(d * scales[1]); frame->token repeat;
      //               z_p[c][f] = m[c][t] + scales[0]*z*exp(logs[c][t])
      // (verified bit-exact vs the original whole-mid onnx on x86)
      // PIPER_DP_KPU=1 takes the interleaved KPU/ORT path instead (see the
      // loader above); it produces the same durations [1,1,79].
      if (dpkpu && !skip_all && !skip_mid) {
        std::normal_distribution<float> ndp(0.f, 1.f);
        // dp noise: drawn c-major (same stream/order as the mid_a path's
        // dpnoise); zf = channel-flipped 0.8*noise — the graph does Mul
        // scales[2] then a channel-reversing Slice (verified: dpfit
        // flows.8/Slice == flip(0.8*noise) bit-exact), so flows.7's
        // x0 = zf[0] = 0.8*noise ch1.
        for (int i = 0; i < 2 * 79; ++i) dp_masked[i] = 0.8f * ndp(rngdp);
        for (int c = 0; c < 2; ++c)
          for (int t = 0; t < 79; ++t)
            dp_zf[c * 79 + t] = dp_masked[(1 - c) * 79 + t];
        int64_t sh_att[9] = {1, 29, 79, 1, 2, 79, 1, 1, 79};
        size_t ln_att[3] = {29 * 79, 2 * 79, 79};
        auto dpdump = [&](const char *nm, const float *p, size_t n) {
          FILE *f = fopen((PD + nm).c_str(), "wb");
          if (f) { fwrite(p, 4, n, f); fclose(f); }
        };
        const bool dpdump_on = getenv("PIPER_DUMP_DP") != nullptr;
        // postnet: (hidden o2, x_mask o1) -> post_out
        dp_post_m->Run({o2.data(), o1.data()}, {dp_post.data()});
        if (dpdump_on) dpdump("dppost.f32", dp_post.data(), 15168);
        // f7 conv: (x0=zf[0], g=post_out, mask) -> h29
        memcpy(dp_x0.data(), dp_zf.data(), 79 * 4);
        dp_f7_m->Run({dp_x0.data(), dp_post.data(), o1.data()},
                     {dp_h29.data()});
        if (dpdump_on) {
          dpdump("dpzf.f32", dp_zf.data(), 158);
          dpdump("dph29_7.f32", dp_h29.data(), 29 * 79);
        }
        {
          float *ins[3] = {dp_h29.data(), dp_zf.data(), o1.data()};
          dp_run1(*att7s, ins, sh_att, ln_att, 3, dp_c7.data());
          if (dpdump_on) dpdump("dpc7.f32", dp_c7.data(), 2 * 79);
        }
        // x0_5 = (c7 * x_mask) flipped channel-wise, take ch0
        for (int c = 0; c < 2; ++c)
          for (int t = 0; t < 79; ++t)
            dp_masked[(1 - c) * 79 + t] = dp_c7[c * 79 + t] * o1[t];
        {
          // f5 kmodel inputs (piper_dp_split.py PIECES order):
          // [flows.5/Split (x0_5), flows.7/Concat_20 (c7), x_mask, Mul_2]
          float *ins[4] = {dp_masked.data(), dp_c7.data(), o1.data(),
                           o2.data()};
          dp_f5_m->Run({ins[0], ins[1], ins[2], ins[3]}, {dp_h29.data()});
          if (dpdump_on) dpdump("dph29_5.f32", dp_h29.data(), 29 * 79);
        }
        {
          float *ins[3] = {dp_h29.data(), dp_c7.data(), o1.data()};
          int64_t sh[9] = {1, 29, 79, 1, 2, 79, 1, 1, 79};
          size_t ln[3] = {29 * 79, 2 * 79, 79};
          dp_run1(*att5s, ins, sh, ln, 3, dp_c5.data());
          if (dpdump_on) dpdump("dpc5.f32", dp_c5.data(), 2 * 79);
        }
        // x0_3 = flip(c5)[0] = c5[1] (verified numerically, no mask here)
        {
          // f3 inputs: [flows.3/Split (x0_3), flows.5/Concat_20 (c5),
          // x_mask, Mul_2]
          memcpy(dp_x0.data(), dp_c5.data() + 79, 79 * 4);
          float *ins[4] = {dp_x0.data(), dp_c5.data(), o1.data(), o2.data()};
          dp_f3_m->Run({ins[0], ins[1], ins[2], ins[3]}, {dp_h29.data()});
          if (dpdump_on) dpdump("dph29_3.f32", dp_h29.data(), 29 * 79);
        }
        {
          float *ins[3] = {dp_h29.data(), dp_c5.data(), o1.data()};
          int64_t sh[9] = {1, 29, 79, 1, 2, 79, 1, 1, 79};
          size_t ln[3] = {29 * 79, 2 * 79, 79};
          dp_run1(*att3s, ins, sh, ln, 3, dp_dur.data());
        }
        const float *dur = dp_dur.data();
        // int8 dp pieces bias the duration logits ~+17% (Ly 64->75 on the
        // 6-syllable test); PIPER_LEN re-centers total length (multiplier).
        float lenfac = scales[1];
        if (const char *le = getenv("PIPER_LEN")) lenfac = (float)atof(le);
        std::fill(zp128.begin(), zp128.end(), 0.f);
        std::fill(mask128.begin(), mask128.end(), 0.f);
        Ly = 0;
        for (int t = 0; t < 79; ++t) {
          int d = (int)std::ceil(dur[t] * lenfac - 1e-6f);
          if (d <= 0) continue;
          const float *mt = o0.data() + (size_t)t;
          const float *lt = o0.data() + 15168 + (size_t)t;
          for (int j = 0; j < d && Ly < FT; ++j, ++Ly) {
            float zn = ndp(rngdp);
            mask128[Ly] = 1.f;
            for (int c = 0; c < 192; ++c)
              zp128[(size_t)c * FT + Ly] =
                  mt[(size_t)c * 79] + scales[0] * zn * expf(lt[(size_t)c * 79]);
          }
          if (Ly >= FT) break;
        }
        zp = zp128.data();
        mk = mask128.data();
        if (getenv("PIPER_DUMP_MID")) {
          FILE *fd = fopen((PD + "mid_dur_dp.f32").c_str(), "wb");
          if (fd) { fwrite(dur, 4, 79, fd); fclose(fd); }
        }
        fprintf(stderr, "[piper] mid done Ly=%lld (dp-kpu)\n", (long long)Ly);
        auto tb2 = clk2::now();
        (void)tb2;
      } else if (!skip_all && !skip_mid) {
      std::vector<Ort::Value> fin;
      std::vector<std::vector<int64_t>> fsh;
      auto push_in = [&](const float *p, std::vector<int64_t> sh) {
        fsh.push_back(sh);
        fin.push_back(Ort::Value::CreateTensor<float>(
            pmem, const_cast<float *>(p), (size_t)std::accumulate(
                     sh.begin(), sh.end(), 1, std::multiplies<int64_t>()),
            sh.data(), sh.size()));
      };
      // dp noise: [1,2,79] standard normal (host RNG replaces the graph's
      // RandomNormalLike, which is hoisted to an input in mid_a)
      std::normal_distribution<float> ndist(0.f, 1.f);
      std::vector<float> dpnoise(2 * 79);
      for (auto &v : dpnoise) v = ndist(rngmid);
      push_in(o1.data(), {1, 1, 79});           // x_mask (Cast_1)
      push_in(o0.data(), {1, 192, 79});         // m = H channels 0..191
      push_in(o0.data() + 15168, {1, 192, 79}); // logs = H channels 192..383
      push_in(o2.data(), {1, 192, 79});         // encoder attn hidden
      push_in(o3.data(), {1, 1, 1, 79});        // attn mask (Unsqueeze)
      push_in(scales.data(), {3});
      push_in(dpnoise.data(), {1, 2, 79});
      std::vector<const char *> in_names;
      Ort::AllocatorWithDefaultOptions palloc;
      for (size_t i = 0; i < mid->GetInputCount(); ++i)
        in_names.push_back(mid->GetInputNameAllocated(i, palloc).release());
      std::vector<const char *> out_names;
      for (size_t i = 0; i < mid->GetOutputCount(); ++i)
        out_names.push_back(mid->GetOutputNameAllocated(i, palloc).release());
      auto mo2 = mid->Run(Ort::RunOptions{nullptr}, in_names.data(), fin.data(),
                        fin.size(), out_names.data(), out_names.size());
      mo = std::move(mo2);
      const float *dur = mo[0].GetTensorMutableData<float>();
      // host expansion -> fill the F=128 bucket directly (zp128/mask128)
      std::fill(zp128.begin(), zp128.end(), 0.f);
      std::fill(mask128.begin(), mask128.end(), 0.f);
      Ly = 0;
      for (int t = 0; t < 79; ++t) {
        int d = (int)std::ceil(dur[t] * scales[1] - 1e-6f);
        if (d <= 0) continue;
        const float *mt = o0.data() + (size_t)t;          // m[:, t] stride 79
        const float *lt = o0.data() + 15168 + (size_t)t;  // logs[:, t] stride 79
        for (int j = 0; j < d && Ly < FT; ++j, ++Ly) {
          float zn = ndist(rngmid);
          mask128[Ly] = 1.f;
          for (int c = 0; c < 192; ++c)
            zp128[(size_t)c * FT + Ly] =
                mt[(size_t)c * 79] + scales[0] * zn * expf(lt[(size_t)c * 79]);
        }
        if (Ly >= FT) break;
      }
      zp = zp128.data();
      mk = mask128.data();
      fprintf(stderr, "[piper] mid done Ly=%lld (host expansion)\n",
              (long long)Ly);
      if (getenv("PIPER_DUMP_MID")) {
        // dump in the pre-padding [1,192,Ly] layout for the x86 compare tools
        FILE *fz = fopen((PD + "mid_zp.f32").c_str(), "wb");
        if (fz) {
          std::vector<float> t(192 * (size_t)Ly);
          for (int c = 0; c < 192; ++c)
            memcpy(t.data() + (size_t)c * Ly, zp128.data() + (size_t)c * FT,
                   (size_t)Ly * 4);
          fwrite(t.data(), 4, t.size(), fz);
          fclose(fz);
        }
        FILE *fm = fopen((PD + "mid_mask.f32").c_str(), "wb");
        if (fm) { fwrite(mask128.data(), 4, (size_t)Ly, fm); fclose(fm); }
        const char *bn[] = {"bnd0", "bnd1", "bnd2", "bnd3", "bnd4"};
        const float *bv[] = {o1.data(), o0.data(), o0.data() + 15168,
                             o2.data(), o3.data()};
        for (int q = 0; q < 5; ++q) {
          FILE *fb = fopen((PD + bn[q] + ".f32").c_str(), "wb");
          fwrite(bv[q], 4, (q == 0 || q == 4) ? 79 : 15168, fb);
          fclose(fb);
        }
        FILE *fd = fopen((PD + "mid_dur.f32").c_str(), "wb");
        if (fd) { fwrite(dur, 4, 79, fd); fclose(fd); }
      }
      } else {
        // SKIP mode: read the already-padded tensors dumped earlier
        zf = ReadBinary<float>(PD + "zp128.f32");
        mf = ReadBinary<float>(PD + "mask128.f32");
        if (zf.size() != 192 * FT || mf.size() != FT) {
          fprintf(stderr, "[piper] SKIP files bad: z=%zu m=%zu\n", zf.size(),
                  mf.size());
          return 1;
        }
        zp = zf.data();
        mk = mf.data();
        for (int i = 0; i < FT; ++i)
          if (mk[i] > 0.5f) Ly++;
        fprintf(stderr, "[piper] SKIP mode: Ly=%lld from files\n",
                (long long)Ly);
      }
      auto tc = clk2::now();
      cb1 = cpu_ms_p();
      cc0 = cpu_ms_p();

      // 3. SKIP path only: files are already padded to the bucket. The live
      // mid path wrote zp128/mask128 directly in the host expansion above
      // (per-channel layout, tail zeroed, mask=0 keeps the tail silent).
      if (skip_all || skip_mid) {
        memcpy(zp128.data(), zp, (size_t)(192 * FT) * 4);
        memcpy(mask128.data(), mk, (size_t)FT * 4);
      }

      // 4. flow chain: single-output pieces on PRE-SPLIT boundaries (the
      // nncase Split second output is broken on K230 — see handover)
      // PIPER_DUMP_FLOW2: run the chain twice with per-stage dumps
      // (fs<p>_<pass>.f32) to isolate resident-context / warm-up anomalies.
      {
        int npass = getenv("PIPER_DUMP_FLOW2") ? 2 : 1;
        if (getenv("PIPER_DUMP_FLOW2")) {
          // record exactly what the flow chain receives
          FILE *fz = fopen((PD + "zin.f32").c_str(), "wb");
          if (fz) { fwrite(zp128.data(), 4, 192 * FT, fz); fclose(fz); }
          FILE *fm2 = fopen((PD + "min.f32").c_str(), "wb");
          if (fm2) { fwrite(mask128.data(), 4, FT, fm2); fclose(fm2); }
        }
        for (int pass = 1; pass <= npass; ++pass) {
          flow[0]->Run({zp128.data(), mask128.data()}, {hT.data()});
          flow[1]->Run({hT.data(), mask128.data()}, {hT2.data()});
          flow[2]->Run({hT2.data(), mask128.data()}, {hT.data()});
          flow[3]->Run({hT.data(), mask128.data()}, {dec_in.data()});
          if (pass == npass) fprintf(stderr, "[piper] flow done (pass %d)\n", pass);
          if (getenv("PIPER_DUMP_FLOW2")) {
            const float *st[4] = {hT.data(), hT2.data(), hT.data(), dec_in.data()};
            for (int q = 0; q < 4; ++q) {
              FILE *fs = fopen((PD + "fs" + std::to_string(q) + "_" +
                                std::to_string(pass) + ".f32")
                                   .c_str(),
                               "wb");
              if (fs) { fwrite(st[q], 4, 192 * FT, fs); fclose(fs); }
            }
          }
        }
      }
      fprintf(stderr, "[piper] flow done\n");
      if (fresh_flow) {
        // diagnostic: identical chain with FRESHLY loaded flow pieces
        std::vector<std::unique_ptr<KpuModel>> fg;
        for (int j = 0; j < 4; ++j)
          fg.push_back(std::make_unique<KpuModel>(
              PD + "piper_flow_s" + std::to_string(j) + ".kmodel", "fFlow"));
        fg[0]->Run({zp128.data(), mask128.data()}, {hT.data()});
        fg[1]->Run({hT.data(), mask128.data()}, {hT2.data()});
        fg[2]->Run({hT2.data(), mask128.data()}, {hT.data()});
        fg[3]->Run({hT.data(), mask128.data()}, {dec_in.data()});
        FILE *fs = fopen((PD + "fsfresh.f32").c_str(), "wb");
        if (fs) { fwrite(dec_in.data(), 4, 192 * FT, fs); fclose(fs); }
        fprintf(stderr, "[piper] fresh-flow done\n");
      }
      if (getenv("PIPER_DUMP_MID")) {
        FILE *fd = fopen((PD + "flow_decin.f32").c_str(), "wb");
        fwrite(dec_in.data(), 4, 192 * FT, fd);
        fclose(fd);
      }
      auto td = clk2::now();
      cc1 = cpu_ms_p();
      cd0 = cpu_ms_p();

      // 5. dec resident chain (CT pieces emit the active skip). Both
      // half-buffers must be oversized like the resdec probe (p0 alone
      // writes 256ch×128 = 32768 floats; zp128 is only 192ch and would
      // overflow).
      int ct_done = 0;
      static std::vector<float> half_a(4 * 1024 * 1024), half_b(4 * 1024 * 1024);
      std::copy(dec_in.begin(), dec_in.end(), half_a.begin());
      std::vector<float> *cur = &half_a;
      std::vector<float> *nxt = &half_b;
      for (size_t i = 0; i < dec.size(); ++i) {
        KpuModel &k = *dec[i];
        std::vector<float> &dst = is_ct((int)i) ? sks_pick(sk0, sk1, sk2, ct_done) : *nxt;
        if (k.input_count() >= 2)
          k.Run({cur->data(), sks_pick(sk0, sk1, sk2, ct_done - 1).data()},
                {dst.data()});
        else
          k.Run({cur->data()}, {dst.data()});
        if (is_ct((int)i))
          ct_done++;
        else
          std::swap(cur, nxt);
      }
      auto te = clk2::now();
      cd1 = cpu_ms_p();
      // audio: cur now holds the last piece output [1,1,1,32768]
      size_t nsamp = (size_t)Ly * 256;
      if (nsamp > 32768) nsamp = 32768;
      audio_all.insert(audio_all.end(), cur->data(), cur->data() + nsamp);
      printf("[piper] sentence Ly=%lld: enc %.0f ms, mid %.0f ms, flow %.0f ms, "
             "dec %.0f ms\n", (long long)Ly,
             std::chrono::duration<double, std::milli>(tb - ta).count(),
             std::chrono::duration<double, std::milli>(tc - tb).count(),
             std::chrono::duration<double, std::milli>(td - tc).count(),
             std::chrono::duration<double, std::milli>(te - td).count());
      printf("[piper] CPU: enc %.0f ms, mid %.0f ms, flow %.0f ms, dec %.0f ms "
             "(total %.0f)\n",
             ca1 - ca0, cb1 - cb0, cc1 - cc0, cd1 - cd0, cd1 - ca0);
      t_synth += std::chrono::duration<double, std::milli>(te - ta).count();
    }
    double cp2 = cpu_ms_p();
    if (out_wav.empty()) out_wav = PD + "piper_out.wav";
    sherpa_onnx::WriteWave(out_wav, 22050, audio_all.data(),
                           (int32_t)audio_all.size());
    double wall = std::chrono::duration<double>(t_all1 - t_all0).count();
    double audio_s = audio_all.size() / 22050.0;
    printf("[piper] WAV %s: %.2f s audio\n", out_wav.c_str(), audio_s);
    printf("[piper] TIMING: synth %.0f ms (front %.0f, synth-total %.0f ms), "
           "load %.0f ms CPU\n", t_synth, t_front, wall * 1000, cp1 - cp0);
    printf("[piper] CPU: synth-chain %.0f ms of %.0f ms wall (%.0f%% single "
           "core), total proc CPU %.0f ms\n", cp2 - cp1, wall * 1000,
           100.0 * (cp2 - cp1) / (wall * 1000), cp2 - cp0);
    printf("[piper] RTF (synth wall / audio) = %.3f\n", t_synth / 1000.0 / audio_s);
    if (daemon_mode) {
      printf("DONE %s %.2f %.3f\n", out_wav.c_str(), audio_s,
             t_synth / 1000.0 / audio_s);
      fflush(stdout);
    }
    }  // per-utterance for(;;)
    mmz_shim_release_all();
    _exit(0);
  }

  // ==== probe-chain: run the whole subgen KPU chain from fixed inputs
  // ==== <dir>/z.f32 [192*256], <dir>/m.f32 [256] -> <dir>/chain.f32.
  // ==== Deterministic (no pregen randomness); compare chain.f32 against the
  // ==== x86 ONNX subgen reference on the PC (cos >= 0.98 gate) BEFORE audio.
  if (!probe_chain.empty()) {
    if (melo_subgen.empty()) {
      fprintf(stderr, "--probe-chain needs --melo-subgen=<comma-separated kmodels>\n");
      return 1;
    }
    std::vector<std::string> sub_parts;
    {
      auto groups0 = ParseSubgenGroups(melo_subgen);
      for (auto &g : groups0)
        sub_parts.insert(sub_parts.end(), g.begin(), g.end());
    }
    k230_kpu_init();
    for (size_t p = 0; p < sub_parts.size(); ++p)
      printf("[probe-chain] piece %zu: %s\n", p, sub_parts[p].c_str());
    auto z = ReadBinary<float>(probe_chain + "/z.f32");
    auto m = ReadBinary<float>(probe_chain + "/m.f32");
    if (z.size() != 192 * 256 || m.size() != 256) {
      fprintf(stderr, "probe-chain size mismatch: z=%zu m=%zu\n", z.size(), m.size());
      return 1;
    }
    int64_t sid0 = 0;
    using clk = std::chrono::steady_clock;
    auto cpu_ms = []() {
      struct rusage ru;
      getrusage(RUSAGE_SELF, &ru);
      return (ru.ru_utime.tv_sec + ru.ru_stime.tv_sec) * 1000.0 +
             (ru.ru_utime.tv_usec + ru.ru_stime.tv_usec) / 1000.0;
    };
    // MELO_IN: single-kmodel execution test — read in0 from an arbitrary f32
    // file (any shape; bytes must match the kmodel input), run, write chain.f32
    // (the 2-input (cur,sid) layout used by all dq pieces).
    std::vector<float> ext_in;
    if (getenv("MELO_IN") && !getenv("MELO_RESDEC")) {
      ext_in = ReadBinary<float>(getenv("MELO_IN"));
      if (ext_in.empty()) {
        fprintf(stderr, "MELO_IN empty/unreadable\n");
        return 1;
      }
      std::vector<float> ext_in2;
      if (getenv("MELO_IN2")) ext_in2 = ReadBinary<float>(getenv("MELO_IN2"));
      std::vector<float> ext_in3;
      if (getenv("MELO_IN3")) ext_in3 = ReadBinary<float>(getenv("MELO_IN3"));
      auto t0x = clk::now();
      double cx0 = cpu_ms();
      std::vector<float> yx;
      {
        KpuModel kx(sub_parts[0], "meloX");
        yx.resize(kx.output_bytes(0) / sizeof(float));
        if (kx.input_count() == 3 && !ext_in3.empty())
          kx.Run({ext_in.data(), ext_in2.data(), ext_in3.data()}, {yx.data()});
        else if (kx.input_count() >= 3 && !ext_in2.empty())
          kx.Run({ext_in.data(), ext_in2.data(), &sid0}, {yx.data()});
        else if (kx.input_count() == 2 && !ext_in2.empty())
          kx.Run({ext_in.data(), ext_in2.data()}, {yx.data()});
        else
          kx.Run({ext_in.data(), &sid0}, {yx.data()});
      }
      double cx1 = cpu_ms();
      auto t1x = clk::now();
      FILE *fx = fopen((probe_chain + "/chain.f32").c_str(), "wb");
      if (!fx) { perror("fopen"); return 1; }
      fwrite(yx.data(), 4, yx.size(), fx);
      fclose(fx);
      printf("[probe-chain] MELO_IN run: out=%zu floats in %.0f ms\n", yx.size(),
             std::chrono::duration<double, std::milli>(t1x - t0x).count());
      printf("[probe-chain] CPU in run: %.0f ms\n", cx1 - cx0);
      mmz_shim_release_all();
      _exit(0);
    }
    // MELO_RESDEC: resident (cur,skip) chain for the piper dec piece list —
    // all kmodels already loaded in ksub[], io buffers allocated once, no
    // reload/alloc churn. Piece order must be p0,ct0,p1..p5,ct1,p6..p10,ct2,
    // p11..p15,p16: CT pieces (indices 1,7,13) emit into the active skip
    // buffer, everything else chains cur->cur; 2-input pieces take
    // (cur, active skip).
    if (getenv("MELO_RESDEC")) {
      static const int CT_IDXS[] = {1, 7, 13};
      auto is_ct = [&](int i) {
        for (int c : CT_IDXS)
          if (c == i) return true;
        return false;
      };
      std::vector<float> x0 = ReadBinary<float>(getenv("MELO_IN"));
      if (x0.empty()) {
        fprintf(stderr, "MELO_RESDEC: MELO_IN empty\n");
        return 1;
      }
      std::vector<float> cur(4 * 1024 * 1024, 0.f), tmp(4 * 1024 * 1024, 0.f);
      std::vector<float> sks[3];
      sks[0].resize(128 * 1024);
      sks[1].resize(64 * 8192);
      sks[2].resize(32 * 32768);
      memcpy(cur.data(), x0.data(), std::min(x0.size(), cur.size()) * sizeof(float));
      int ct_done = 0;
      double c0 = cpu_ms();
      std::vector<std::unique_ptr<KpuModel>> rk;
      for (const auto &p : sub_parts) rk.push_back(std::make_unique<KpuModel>(p, "resdec"));
      double c1 = cpu_ms();
      auto t0r = clk::now();
      for (size_t i = 0; i < rk.size(); ++i) {
        KpuModel &k = *rk[i];
        std::vector<float> &dst = is_ct((int)i) ? sks[ct_done] : tmp;
        auto ta = clk::now();
        if (k.input_count() >= 2)
          k.Run({cur.data(), sks[ct_done - 1].data()}, {dst.data()});
        else
          k.Run({cur.data()}, {dst.data()});
        auto tb = clk::now();
        printf("[resdec] piece %zu: %.0f ms\n", i,
               std::chrono::duration<double, std::milli>(tb - ta).count());
        if (is_ct((int)i))
          ct_done++;
        else
          cur.swap(tmp);
      }
      auto t1r = clk::now();
      double c2 = cpu_ms();
      printf("[resdec] TOTAL: %.0f ms\n",
             std::chrono::duration<double, std::milli>(t1r - t0r).count());
      printf("[resdec] CPU: load %.0f ms, chain %.0f ms\n", c1 - c0, c2 - c1);
      FILE *fx = fopen((probe_chain + "/resdec_out.f32").c_str(), "wb");
      if (fx) {
        fwrite(cur.data(), 4, 32768, fx);
        fclose(fx);
      }
      mmz_shim_release_all();
      _exit(0);
    }
    // MELO_NPIECES: bisect aid — run only the first N pieces of the chain and
    // dump every piece output to <dir>/pc<k>.f32 for piece-wise cos vs ORT.
    // k=0: flow_a out, k=1: flow_b out, k>=2: dq(p-2) out.
    int npieces = (int)sub_parts.size();
    if (getenv("MELO_NPIECES")) npieces = atoi(getenv("MELO_NPIECES"));
    auto dump_piece = [&](int k, const std::vector<float> &v) {      if (k >= npieces) return;
      std::string pf = probe_chain + "/pc" + std::to_string(k) + ".f32";
      FILE *pf2 = fopen(pf.c_str(), "wb");
      if (pf2) { fwrite(v.data(), 4, v.size(), pf2); fclose(pf2); }
    };
    auto t0 = clk::now();
    std::vector<float> y;
    auto groups = ParseSubgenGroups(melo_subgen);
    if (sub_parts.size() == 1) {
      y = KpuRunScopedFlow(sub_parts[0], "meloP", z, m, sid0);
    } else if (sub_parts.size() == 7 || groups.size() == 2) {
      // (cur,sid) chains: dq pieces, optionally split by '|' with a CPU
      // splice (--melo-midcpu); first cur comes from <dir>/in0.f32 (z_hat)
      std::vector<float> in0 = ReadBinary<float>(probe_chain + "/in0.f32");
      if (in0.size() != 192 * 256) {
        fprintf(stderr, "probe-chain in0.f32 size %zu != 192*256\n", in0.size());
        return 1;
      }
      Ort::Env menv(ORT_LOGGING_LEVEL_WARNING, "meloP");
      Ort::SessionOptions mso;
      mso.SetIntraOpNumThreads(1);
      mso.SetInterOpNumThreads(1);
      std::unique_ptr<Ort::Session> mid_ses;
      if (!melo_midcpu.empty())
        mid_ses = std::make_unique<Ort::Session>(menv, melo_midcpu.c_str(), mso);
      std::vector<std::string> gB;
      if (groups.size() == 2) gB = groups[1];
      y = RunChainKpuCpuKpu(groups[0], gB, mid_ses.get(), in0, sid0);
    } else {
      // mirrors --melo-say 9-piece path: flow_a(zp,mk,sid) -> flow_b(o1,mk,sid)
      // -> dq0..dq6 sequential (cur,sid); one kmodel resident at a time
      std::vector<float> o1 = KpuRunScopedFlow(sub_parts[0], "meloP0", z, m, sid0);
      dump_piece(0, o1);
      std::vector<float> cur = KpuRunScopedFlow(sub_parts[1], "meloP1", o1, m, sid0);
      dump_piece(1, cur);
      o1.clear(); o1.shrink_to_fit();
      for (size_t p = 2; p < sub_parts.size(); ++p) {
        cur = KpuRunScoped(sub_parts[p], "meloP2", {cur.data(), &sid0});
        dump_piece((int)p, cur);
      }
      y.swap(cur);
    }
    auto t1 = clk::now();
    FILE *f = fopen((probe_chain + "/chain.f32").c_str(), "wb");
    if (!f) { perror("fopen"); return 1; }
    fwrite(y.data(), 4, y.size(), f);
    fclose(f);
    printf("[probe-chain] wrote chain.f32 (%zu floats, %.1f ms audio) in %.0f ms\n",
           y.size(), y.size() / 44100.0 * 1000.0,
           std::chrono::duration<double, std::milli>(t1 - t0).count());
    mmz_shim_release_all();
    _exit(0);
  }

  // ==== melo say: text -> lexicon frontend -> pregen (CPU ORT) -> subgen
  // ==== (KPU, F=256 bucket) -> 44100Hz wav
  if (!melo_say.empty()) {
    if (melo_pregen.empty() || melo_subgen.empty() || melo_lexicon.empty() ||
        melo_tokens.empty()) {
      fprintf(stderr, "--melo-say needs --melo-pregen/--melo-subgen/--melo-lexicon/--melo-tokens\n");
      return 1;
    }
    using clk = std::chrono::steady_clock;
    auto ms = [](clk::time_point a, clk::time_point b) {
      return std::chrono::duration<double, std::milli>(b - a).count();
    };
    k230_kpu_init();
    Ort::Env menv(ORT_LOGGING_LEVEL_WARNING, "melo");
    Ort::SessionOptions mso;
    mso.SetIntraOpNumThreads(1);
    mso.SetInterOpNumThreads(1);
    auto mem = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);
    Ort::Session pre(menv, melo_pregen.c_str(), mso);
    // optional CPU flow (z_pre -> z_hat); kept on CPU because the K230 GNNE
    // mis-executes the attention MatMuls inside the flow
    std::unique_ptr<Ort::Session> flow_ses;
    if (!melo_flow.empty()) {
      // NOTE: 8-thread midcpu measured WORSE than 1 thread (56s vs 49s) —
      // board ORT fp32 conv is scalar + memory-bound; keep 1 thread
      Ort::SessionOptions fso;
      fso.SetIntraOpNumThreads(1);
      fso.SetInterOpNumThreads(1);
      flow_ses = std::make_unique<Ort::Session>(menv, melo_flow.c_str(), fso);
    }
    std::unique_ptr<Ort::Session> mid_ses;
    if (!melo_midcpu.empty()) {
      Ort::SessionOptions cso;
      cso.SetIntraOpNumThreads(1);
      cso.SetInterOpNumThreads(1);
      mid_ses = std::make_unique<Ort::Session>(menv, melo_midcpu.c_str(), cso);
    }
    // subgen may be ONE kmodel, several comma-separated pieces, or two
    // '|' separated KPU groups around a --melo-midcpu CPU splice
    std::vector<std::string> sub_parts;
    auto groups0 = ParseSubgenGroups(melo_subgen);
    for (auto &g : groups0)
      sub_parts.insert(sub_parts.end(), g.begin(), g.end());
    const bool spliced = groups0.size() == 2;
    if ((!spliced && sub_parts.size() != 1 && sub_parts.size() != 4 &&
         sub_parts.size() != 6 && sub_parts.size() != 7 && sub_parts.size() != 9) ||
        (spliced && (groups0[0].empty() || groups0[1].empty()))) {
      fprintf(stderr, "--melo-subgen must be 1, 4, 6, 7 or 9 comma-separated kmodels\n");
      return 1;
    }
    std::vector<std::unique_ptr<KpuModel>> ksub;
    // all layouts keep their pieces resident: reloading per sentence costs
    // ~15s file IO per piece with cold caches
    // composite entries "dq4chunk"/"dq5chunk" expand to per-chunk kmodels
    // (the >32768-width GNNE zero bug forces resblock stages to run chunked
    // in separate kmodels — the compiler re-fuses slices inside ONE model)
    struct ChunkStage {
      int kind = 0;                       // 4 or 5
      std::vector<std::unique_ptr<KpuModel>> ms;  // 4: [head,c0,c1,c2] 5: [c0..c5]
      std::vector<int> out_w;             // chunk output widths (frames)
    };
    std::map<int, ChunkStage> compos;     // sub_parts index -> stage
    for (size_t pi = 0; pi < sub_parts.size(); ++pi) {
      const std::string &p = sub_parts[pi];
      if (p == "dq4chunk" || p == "dq5chunk") {
        ChunkStage st;
        st.kind = p == "dq4chunk" ? 4 : 5;
        auto load = [&](const char *nm) {
          st.ms.push_back(std::make_unique<KpuModel>(nm, "meloSub"));
        };
        if (st.kind == 4) {
          load("./melo_dq4h.kmodel");
          load("./melo_dq4hc0.kmodel");
          load("./melo_dq4hc1.kmodel");
          load("./melo_dq4hc2.kmodel");
          st.out_w = {21845, 21846, 21845};
        } else {
          for (int ci = 0; ci < 6; ++ci) {
            char nm[64];
            snprintf(nm, sizeof nm, "./melo_dq5hc%d.kmodel", ci);
            load(nm);
          }
          st.out_w = {21845, 21846, 21845, 21845, 21846, 21845};
        }
        compos[(int)pi] = std::move(st);
        ksub.push_back(nullptr);          // placeholder keeps indices aligned
      } else {
        ksub.push_back(std::make_unique<KpuModel>(p, "meloSub"));
      }
    }

    std::unordered_map<std::string, int64_t> tok2id;
    {
      std::ifstream tf(melo_tokens);
      std::string line;
      while (std::getline(tf, line)) {
        if (!line.empty() && line.back() == '\r') line.pop_back();
        size_t sp = line.find(' ');
        if (sp != std::string::npos)
          tok2id[line.substr(0, sp)] = std::stoll(line.substr(sp + 1));
      }
    }
    // lexicon: word -> (phone ids, tone ids); halves format
    struct WordEntry { std::vector<int64_t> ids, tones; };
    std::vector<std::pair<std::string, WordEntry>> lex;
    {
      std::ifstream lf(melo_lexicon);
      std::string line;
      while (std::getline(lf, line)) {
        if (!line.empty() && line.back() == '\r') line.pop_back();
        std::istringstream iss(line);
        std::string w;
        iss >> w;
        for (auto &c : w) c = (char)tolower((unsigned char)c);
        std::vector<std::string> parts;
        std::string p;
        while (iss >> p) parts.push_back(p);
        if (parts.size() < 2 || (parts.size() & 1)) continue;
        size_t n = parts.size() / 2;
        WordEntry e;
        bool ok = true;
        for (size_t i = 0; i < n; ++i) {
          auto it = tok2id.find(parts[i]);
          if (it == tok2id.end()) { ok = false; break; }
          e.ids.push_back(it->second);
          e.tones.push_back(std::stoll(parts[n + i]));
        }
        if (ok && !e.ids.empty()) lex.emplace_back(w, std::move(e));
      }
      std::sort(lex.begin(), lex.end(),
                [](auto &a, auto &b) { return a.first.size() > b.first.size(); });
    }
    printf("[melo] tokens=%zu lexicon=%zu\n", tok2id.size(), lex.size());

    float ns = 0.667f, ls = 1.0f, nsw = 0.8f;
    if (getenv("MELO_NS")) ns = atof(getenv("MELO_NS"));
    if (getenv("MELO_LS")) ls = atof(getenv("MELO_LS"));
    if (getenv("MELO_NSW")) nsw = atof(getenv("MELO_NSW"));

    // frontend: greedy longest-match (byte-string) + punctuation split.
    // Punctuation is NEVER fed to the model (sherpa's melo frontend drops it;
    // feeding punct tokens garbles the audio). 。！？ end a sentence; ，、；：
    // are recorded as fallback split points, used only when a sentence would
    // exceed the F=256 frame bucket.
    struct MeloSentence {
      std::vector<std::pair<int64_t, int64_t>> ids;  // (phone id, tone)
      std::vector<size_t> fallbacks;                 // comma split positions
    };
    std::vector<MeloSentence> sents;
    {
      MeloSentence cur;
      // second field: 1 = ends a sentence, 0 = fallback split point only
      static const std::pair<const char *, int> PUNCTS[] = {
          {"。", 1}, {"！", 1}, {"？", 1},
          {"，", 0}, {"、", 0}, {"；", 0}, {"：", 0}};
      const std::string &text = melo_say;
      size_t i = 0;
      while (i < text.size()) {
        bool matched = false;
        for (const auto &w : lex) {
          if (w.first.size() < 2 * 2) break;   // only multi-char words here
          if (text.compare(i, w.first.size(), w.first) == 0) {
            for (size_t k = 0; k < w.second.ids.size(); ++k)
              cur.ids.push_back({w.second.ids[k], w.second.tones[k]});
            i += w.first.size();
            matched = true;
            break;
          }
        }
        if (matched) continue;
        // single UTF-8 char
        unsigned char c = text[i];
        size_t cl = c < 0x80 ? 1 : (c < 0xE0 ? 2 : (c < 0xF0 ? 3 : 4));
        std::string ch = text.substr(i, cl);
        bool single = false;
        for (const auto &w : lex) {
          if (w.first == ch) {
            for (size_t k = 0; k < w.second.ids.size(); ++k)
              cur.ids.push_back({w.second.ids[k], w.second.tones[k]});
            single = true;
            break;
          }
        }
        if (!single) {
          bool is_punct = false;
          for (const auto &pp : PUNCTS) {
            if (ch == pp.first) {
              if (pp.second) {
                if (!cur.ids.empty()) { sents.push_back(std::move(cur)); cur = MeloSentence{}; }
              } else if (!cur.ids.empty()) {
                cur.fallbacks.push_back(cur.ids.size());
              }
              is_punct = true;
              break;
            }
          }
          if (!is_punct && cl == 1) {
            auto it = tok2id.find(ch);
            if (it != tok2id.end()) cur.ids.push_back({it->second, 0});
          }
        }
        i += cl;
      }
      if (!cur.ids.empty()) sents.push_back(std::move(cur));
    }
    // A sentence longer than the frame bucket (T grows ~6.5 frames/phone
    // with add_blank; F=256 caps at ~36 phones) is pre-split at comma
    // fallbacks; the nsw retry below covers the remaining random overflow
    {
      const size_t MAX_PH = 36;
      std::vector<MeloSentence> split;
      for (auto &s : sents) {
        if (s.ids.size() <= MAX_PH) { split.push_back(std::move(s)); continue; }
        size_t start = 0;
        while (start < s.ids.size()) {
          size_t cut = s.ids.size();
          for (size_t fb : s.fallbacks)
            if (fb > start && fb - start <= MAX_PH) cut = fb;
          if (cut - start > MAX_PH) cut = start + MAX_PH;
          MeloSentence part;
          part.ids.assign(s.ids.begin() + start, s.ids.begin() + cut);
          split.push_back(std::move(part));
          start = cut;
        }
      }
      sents.swap(split);
    }
    printf("[melo] %zu sentences\n", sents.size());

    const int F = 256;
    std::vector<float> audio_out;
    double t_pre = 0, t_kpu = 0;
    int64_t one[1] = {1};
    for (size_t si = 0; si < sents.size(); ++si) {
      const size_t L = sents[si].ids.size();
      if (L == 0) continue;
      int64_t sid0 = 0;
      // add_blank=1 model metadata: interleave blank id 0 between tokens
      // (x and tones alike). Feeding tokens without blanks halves every
      // duration and garbles the audio — this was the gibberish bug.
      const int64_t Lb = (int64_t)(2 * L + 1);
      std::vector<int64_t> ids(Lb, 0), tones(Lb, 0);
      for (size_t j = 0; j < L; ++j) {
        ids[2 * j + 1] = sents[si].ids[j].first;
        tones[2 * j + 1] = sents[si].ids[j].second;
      }
      int64_t shp2[2] = {1, Lb};
      int64_t xl = Lb;
      const char *pin[] = {"x", "x_lengths", "tones", "sid", "noise_scale", "length_scale", "noise_scale_w"};
      std::vector<Ort::Value> vi;
      vi.push_back(Ort::Value::CreateTensor<int64_t>(mem, ids.data(), Lb, shp2, 2));
      vi.push_back(Ort::Value::CreateTensor<int64_t>(mem, &xl, 1, one, 1));
      vi.push_back(Ort::Value::CreateTensor<int64_t>(mem, tones.data(), Lb, shp2, 2));
      vi.push_back(Ort::Value::CreateTensor<int64_t>(mem, &sid0, 1, one, 1));
      vi.push_back(Ort::Value::CreateTensor<float>(mem, &ns, 1, one, 1));
      vi.push_back(Ort::Value::CreateTensor<float>(mem, &ls, 1, one, 1));
      vi.push_back(Ort::Value::CreateTensor<float>(mem, &nsw, 1, one, 1));
      const char *pout[] = {"/Add_2_output_0", "/Cast_4_output_0"};
      auto a1 = clk::now();
      std::vector<Ort::Value> out = pre.Run(Ort::RunOptions{nullptr}, pin, vi.data(), 7, pout, 2);
      int64_t T = out[0].GetTensorTypeAndShapeInfo().GetShape()[2];
      if (T > F) {
        // duration-predictor noise can push T past the frame bucket; retry
        // with less duration noise — mild prosody change beats truncation
        for (float nsw2 : {0.3f, 0.0f}) {
          vi[6] = Ort::Value::CreateTensor<float>(mem, &nsw2, 1, one, 1);
          out = pre.Run(Ort::RunOptions{nullptr}, pin, vi.data(), 7, pout, 2);
          T = out[0].GetTensorTypeAndShapeInfo().GetShape()[2];
          if (T <= F) break;
        }
      }
      t_pre += ms(a1, clk::now());
      const float *zp0 = out[0].GetTensorData<float>();
      const float *mk0 = out[1].GetTensorData<float>();
      if (T > F) { printf("[melo] sent %zu T=%ld > F, truncated\n", si, (long)T); }
      const int64_t Tc = std::min<int64_t>(T, F);
      std::vector<float> zp(192 * F, 0.f), mk(F, 0.f);
      for (int64_t t = 0; t < Tc; ++t)
        for (int ch = 0; ch < 192; ++ch) zp[ch * F + t] = zp0[ch * T + t];
      for (int64_t t = 0; t < Tc; ++t) mk[t] = mk0[t];
      if (flow_ses) {
        // CPU flow on the padded F-frame window: z_pre+mask+sid -> z_hat
        int64_t shp_flow[3] = {1, 192, F};
        int64_t shp_mask[3] = {1, 1, F};
        const char *fin[] = {"/Add_2_output_0", "/Cast_4_output_0", "sid"};
        const char *fout[] = {"/Mul_10_output_0"};
        std::vector<Ort::Value> fin_v;
        fin_v.push_back(Ort::Value::CreateTensor<float>(mem, zp.data(), zp.size(), shp_flow, 3));
        fin_v.push_back(Ort::Value::CreateTensor<float>(mem, mk.data(), mk.size(), shp_mask, 3));
        fin_v.push_back(Ort::Value::CreateTensor<int64_t>(mem, &sid0, 1, one, 1));
        auto fo = flow_ses->Run(Ort::RunOptions{nullptr}, fin, fin_v.data(), 3, fout, 1);
        std::vector<float> zh(192 * F, 0.f);
        memcpy(zh.data(), fo[0].GetTensorData<float>(), zh.size() * sizeof(float));
        zp.swap(zh);
      }
      auto a2 = clk::now();
      // F=256 frame bucket: final audio is 512 samples/frame (dq6 out);
      // other layouts query their first/last kmodel IO sizes
      auto groups = ParseSubgenGroups(melo_subgen);
      const bool chain_layout =
          sub_parts.size() == 9 || sub_parts.size() == 7 || groups.size() == 2;
      std::vector<float> y(chain_layout ? 256u * 512u
                                        : std::max(ksub[0]->output_bytes(0),
                                                   ksub.back()->output_bytes(0)) /
                                              sizeof(float));
      if (ksub.size() == 1 && compos.empty()) {
        ksub[0]->Run({zp.data(), mk.data(), &sid0}, {y.data()});
      } else if (compos.size() || groups.size() == 2 || sub_parts.size() == 7) {
        // (cur,sid) pieces with an optional mid-chain CPU splice and
        // optional composite chunk-stages (dq4chunk/dq5chunk)
        using clk2 = std::chrono::steady_clock;
        std::vector<float> cur(zp);
        size_t idx = 0;
        auto run_piece = [&](size_t pi, std::vector<float> &c) {
          auto cit = compos.find((int)pi);
          if (cit != compos.end()) {
            ChunkStage &st = cit->second;
            auto t0 = clk2::now();
            if (st.kind == 4) {
              // head: (cur,sid) -> ct_out(32x65536); chunks feed FULL ct_out
              std::vector<float> ct(st.ms[0]->output_bytes(0) / sizeof(float));
              st.ms[0]->Run({c.data(), &sid0}, {ct.data()});
              const int C = 32, W = 65536;
              std::vector<float> lr4((size_t)C * W);
              size_t off = 0;
              for (int ci = 0; ci < 3; ++ci) {
                std::vector<float> oc(st.ms[1 + ci]->output_bytes(0) / sizeof(float));
                st.ms[1 + ci]->Run({ct.data(), &sid0}, {oc.data()});
                size_t ow = oc.size() / C;
                for (int ch = 0; ch < C; ++ch)
                  memcpy(&lr4[(size_t)ch * W + off], &oc[(size_t)ch * ow],
                         ow * sizeof(float));
                off += ow;
              }
              c.swap(lr4);
            } else {
              const int C = 16, W = 131072;
              std::vector<float> lr5((size_t)C * W);
              size_t acc = 0;
              for (int ci = 0; ci < 6; ++ci) {
                size_t bytes = st.ms[ci]->output_bytes(0);
                std::vector<float> oc(bytes / sizeof(float));
                st.ms[ci]->Run({c.data(), &sid0}, {oc.data()});
                size_t ow = oc.size() / C;
                for (int ch = 0; ch < C; ++ch)
                  memcpy(&lr5[(size_t)ch * W + acc], &oc[(size_t)ch * ow],
                         ow * sizeof(float));
                acc += ow;
              }
              c.swap(lr5);
            }
            fprintf(stderr, "[chain-t] %s composite %.1f ms\n",
                    st.kind == 4 ? "dq4chunk" : "dq5chunk",
                    std::chrono::duration<double, std::milli>(clk2::now() - t0).count());
          } else {
            auto t0 = clk2::now();
            std::vector<float> nxt(ksub[pi]->output_bytes(0) / sizeof(float));
            ksub[pi]->Run({c.data(), &sid0}, {nxt.data()});
            fprintf(stderr, "[chain-t] %s %.1f ms\n", ksub[pi]->path().c_str(),
                    std::chrono::duration<double, std::milli>(clk2::now() - t0).count());
            c.swap(nxt);
          }
        };
        size_t nA = groups.size() == 2 ? groups[0].size() : sub_parts.size();
        for (size_t p = 0; p < nA; ++p) run_piece(idx++, cur);
        if (mid_ses) {
          auto t0 = clk2::now();
          Ort::MemoryInfo mem2 = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);
          auto ish = mid_ses->GetInputTypeInfo(0).GetTensorTypeAndShapeInfo().GetShape();
          int64_t shp[3] = {1, ish[1], ish[2]};
          int64_t one2[1] = {1};
          const char *in_names[] = {"/dec/LeakyRelu_3_output_0", "sid"};
          const char *out_names[] = {"/dec/LeakyRelu_5_output_0"};
          std::vector<Ort::Value> iv;
          iv.push_back(Ort::Value::CreateTensor<float>(mem2, (float *)cur.data(), cur.size(), shp, 3));
          iv.push_back(Ort::Value::CreateTensor<int64_t>(mem2, &sid0, 1, one2, 1));
          auto ov = mid_ses->Run(Ort::RunOptions{nullptr}, in_names, iv.data(), 2, out_names, 1);
          size_t n = (size_t)ov[0].GetTensorTypeAndShapeInfo().GetElementCount();
          std::vector<float> out2(n);
          memcpy(out2.data(), ov[0].GetTensorData<float>(), n * sizeof(float));
          fprintf(stderr, "[chain-t] MIDCPU %.1f ms\n",
                  std::chrono::duration<double, std::milli>(clk2::now() - t0).count());
          cur.swap(out2);
        }
        if (groups.size() == 2)
          for (size_t p = 0; p < groups[1].size(); ++p) run_piece(idx++, cur);
        y.swap(cur);
      } else if (sub_parts.size() == 9) {
        // 9-piece: flow_a, flow_b, dq0..dq6 — after flow_b every piece is
        // (prev_out, sid) — pure sequential, no mask/mul10 skips; one kmodel
        // resident at a time
        std::vector<float> o1 = KpuRunScopedFlow(sub_parts[0], "meloP0", zp, mk, sid0);
        std::vector<float> cur = KpuRunScopedFlow(sub_parts[1], "meloP1", o1, mk, sid0);
        for (size_t p = 2; p < sub_parts.size(); ++p)
          cur = KpuRunScoped(sub_parts[p], "meloP2", {cur.data(), &sid0});
        y.swap(cur);
      } else if (sub_parts.size() == 7) {
        // CPU-flow layout: dq0..dq6 sequential (cur,sid); cur starts as z_hat
        std::vector<float> cur = KpuRunScoped(sub_parts[0], "meloP0", {zp.data(), &sid0});
        for (size_t p = 1; p < sub_parts.size(); ++p)
          cur = KpuRunScoped(sub_parts[p], "meloP2", {cur.data(), &sid0});
        y.swap(cur);
      } else {
        // multi-piece chain (4: fa,fb,da,db | 6: fa,fb,d1,d2a,d2b,d3)
        std::vector<float> o1(ksub[0]->output_bytes(0) / sizeof(float));
        ksub[0]->Run({zp.data(), mk.data(), &sid0}, {o1.data()});          // flow_a
        std::vector<float> o2(ksub[1]->output_bytes(0) / sizeof(float));
        ksub[1]->Run({o1.data(), mk.data(), &sid0}, {o2.data()});          // flow_b -> mul10
        if (ksub.size() == 4) {
          std::vector<float> o3(ksub[2]->output_bytes(0) / sizeof(float));
          ksub[2]->Run({o2.data(), &sid0}, {o3.data()});                   // dec_a
          ksub[3]->Run({o3.data(), o2.data(), &sid0}, {y.data()});         // dec_b
        } else {
          std::vector<float> o3(ksub[2]->output_bytes(0) / sizeof(float));
          ksub[2]->Run({o2.data(), &sid0}, {o3.data()});                   // d1
          std::vector<float> o4(ksub[3]->output_bytes(0) / sizeof(float));
          ksub[3]->Run({o3.data(), o2.data(), &sid0}, {o4.data()});        // d2a
          std::vector<float> o5(ksub[4]->output_bytes(0) / sizeof(float));
          ksub[4]->Run({o4.data(), o2.data(), &sid0}, {o5.data()});        // d2b
          ksub[5]->Run({o5.data(), o2.data(), &sid0}, {y.data()});         // d3
        }
      }
      t_kpu += ms(a2, clk::now());
      const int64_t n = Tc * 512;
      for (int64_t j = 0; j < n && j < (int64_t)y.size(); ++j) audio_out.push_back(y[j]);
      printf("  [melo] sent %zu: %zu tok -> T=%ld -> %.2fs\n", si, L, (long)T, n / 44100.0);
    }
    sherpa_onnx::WriteWave(out_wav, 44100, audio_out.data(), audio_out.size());
    const double audio_s = (double)audio_out.size() / 44100.0;
    printf("[melo] DONE %s audio=%.2fs pre=%.0fms kpu=%.0fms RTF=%.3f\n",
           out_wav.c_str(), audio_s, t_pre, t_kpu, (t_pre + t_kpu) / 1000.0 / audio_s);
    mmz_shim_release_all();
    _exit(0);
  }

  // ==== matcha probe: acoustic (CPU) + vocoder ORT vs KPU comparison ====
  // ==== matcha say: text -> (greedy longest-match lexicon) -> acoustic CPU
  // ==== -> KPU vocoder -> 22050Hz wav. Needs --matcha-acoustic/kmodel/lexicon.
  if (!matcha_say.empty() ||
      (daemon_mode && (!matcha_a_kmodel.empty() || !matcha_acoustic.empty()))) {
    if ((matcha_acoustic.empty() && matcha_a_kmodel.empty()) ||
        (matcha_kmodel.empty() && matcha_vocoder.empty()) ||
        matcha_lexicon.empty() || matcha_tokens.empty()) {
      fprintf(stderr, "--matcha-say needs acoustic|a-kmodel + (kmodel|vocoder) + lexicon/tokens\n");
      return 1;
    }
    const bool voc_is_ort = matcha_kmodel.empty();
    const bool voc_dual = !voc_is_ort && getenv("MATCHA_DUAL") != nullptr;
    // full-KPU mode: A-kmodel (encoder+DP) + CPU one-hot + C128 windows
    const bool abc_mode = !matcha_a_kmodel.empty() &&
                         (!matcha_c_kmodel.empty() || !matcha_c_onnx.empty());
    // est mode: A+B on CPU (matcha_ab.onnx via --matcha-acoustic) + est128
    // single-step decoder kmodel (3 Euler calls per 128-frame window)
    const bool est_mode = !matcha_est_kmodel.empty();
    using clk = std::chrono::steady_clock;
    auto ms = [](clk::time_point a, clk::time_point b) {
      return std::chrono::duration<double, std::milli>(b - a).count();
    };
    k230_kpu_init();
    Ort::Env menv(ORT_LOGGING_LEVEL_WARNING, "matcha");
    Ort::SessionOptions mso;
    mso.SetIntraOpNumThreads(1);
    mso.SetInterOpNumThreads(1);
    auto mem = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);
    std::unique_ptr<Ort::Session> aco;
    if (!matcha_acoustic.empty())
      aco = std::make_unique<Ort::Session>(menv, matcha_acoustic.c_str(), mso);
    std::unique_ptr<KpuModel> kvoc;
    std::unique_ptr<Ort::Session> ovoc;
    std::unique_ptr<KpuModel> kA, kC, kEst;
    std::unique_ptr<Ort::Session> cOrt;
    if (est_mode) {
      if (matcha_acoustic.empty() && matcha_a_kmodel.empty()) {
        fprintf(stderr, "--matcha-est-kmodel needs --matcha-acoustic=<matcha_ab.onnx> or --matcha-a-kmodel\n");
        return 1;
      }
      kEst = std::make_unique<KpuModel>(matcha_est_kmodel, "matchaEst");
      if (!matcha_a_kmodel.empty())
        kA = std::make_unique<KpuModel>(matcha_a_kmodel, "matchaA");
    } else if (abc_mode) {
      kA = std::make_unique<KpuModel>(matcha_a_kmodel, "matchaA");
      if (!matcha_c_kmodel.empty())
        kC = std::make_unique<KpuModel>(matcha_c_kmodel, "matchaC");
      else
        cOrt = std::make_unique<Ort::Session>(menv, matcha_c_onnx.c_str(), mso);
    }
    if (voc_is_ort || voc_dual)
      ovoc = std::make_unique<Ort::Session>(menv, matcha_vocoder.c_str(), mso);
    if (!voc_is_ort)
      kvoc = std::make_unique<KpuModel>(matcha_kmodel, "hifigan");
    // optional per-band mel equalization for the folded vocoder kmodel
    std::vector<float> eq_c, eq_s;
    if (!voc_is_ort) {
      const std::string dir = matcha_kmodel.substr(0, matcha_kmodel.find_last_of('/'));
      FILE *f = fopen((dir + "/mel_eq.bin").c_str(), "rb");
      if (f) {
        std::vector<float> buf(160);
        if (fread(buf.data(), 4, 160, f) == 160) {
          eq_c.assign(buf.begin(), buf.begin() + 80);
          eq_s.assign(buf.begin() + 80, buf.end());
        }
        fclose(f);
      }
      printf("[matcha] mel_eq: %s\n", eq_c.empty() ? "none" : "loaded");
    }

    // lexicon: word -> token ids (longest-match segmentation over UTF-8)
    std::unordered_map<std::string, int64_t> tok2id;
    {
      // line-based: the token itself may be a single space (" 0"), which
      // stream extraction (>>) cannot represent
      std::ifstream tf(matcha_tokens);
      std::string line;
      while (std::getline(tf, line)) {
        if (!line.empty() && line.back() == '\r') line.pop_back();
        size_t lastsp = line.find_last_of(' ');
        if (lastsp == std::string::npos) continue;
        std::string tok = line.substr(0, lastsp);
        try {
          int64_t id = std::stoll(line.substr(lastsp + 1));
          tok2id[tok] = id;
        } catch (...) {}
      }
      printf("[matcha] tokens=%zu\n", tok2id.size());
    }
    std::unordered_map<std::string, std::vector<int64_t>> w2ids;
    std::vector<std::string> words_sorted;  // for greedy max match
    {
      std::ifstream lf(matcha_lexicon);
      std::string line;
      while (std::getline(lf, line)) {
        size_t sp = line.find(' ');
        if (sp == std::string::npos) continue;
        std::string w = line.substr(0, sp);
        std::vector<int64_t> ids;
        std::istringstream rest(line.substr(sp + 1));
        std::string syl;
        while (rest >> syl) {
          auto it = tok2id.find(syl);
          if (it != tok2id.end()) ids.push_back(it->second);
        }
        if (!ids.empty() && w2ids.find(w) == w2ids.end()) {
          w2ids[w] = ids;
          words_sorted.push_back(w);
        }
      }
      std::sort(words_sorted.begin(), words_sorted.end(),
                [](const std::string &a, const std::string &b) {
                  return a.size() > b.size();
                });
      printf("[matcha] lexicon words=%zu\n", words_sorted.size());
    }

    float ns = 1.0f, ls = 1.0f;  // sherpa matcha defaults (NOT vits' 0.667!)
    if (getenv("MATCHA_NS")) ns = atof(getenv("MATCHA_NS"));
    if (getenv("MATCHA_LS")) ls = atof(getenv("MATCHA_LS"));
    // one utterance per call — shared by --matcha-say and the --daemon loop
    // (models/lexicon above stay resident across daemon utterances)
    auto synth_matcha = [&](const std::string &m_text,
                            const std::string &m_out) -> bool {
    // split text into sentences at 。！？；，tokenize each via greedy match.
    // sherpa convention (matcha-tts-lexicon.cc + OfflineTtsImpl::AddBlank):
    //   - punctuation maps to tokens (，→, 。→. ！→! ？→? ；→;) appended to the
    //     sentence before splitting
    //   - every sentence then gets pad tokens interleaved: [pad, t0, pad, t1,
    //     ..., pad] with pad = '_' (id 1, from model metadata pad_id)
    // without the interleaving the acoustic sees an out-of-distribution
    // token stream and produces gibberish mel.
    std::vector<std::vector<int64_t>> sents;
    {
      std::vector<int64_t> cur;
      const std::string &t = m_text;
      size_t i = 0;
      while (i < t.size()) {
        // try longest lexicon word at i (bytes; entries are whole UTF-8 words)
        bool matched = false;
        for (const auto &w : words_sorted) {
          if (w.size() <= t.size() - i && t.compare(i, w.size(), w) == 0) {
            const auto &ids = w2ids[w];
            cur.insert(cur.end(), ids.begin(), ids.end());
            i += w.size();
            matched = true;
            break;
          }
        }
        if (!matched) {
          // punctuation: map to the ASCII token, append, and split the sentence.
          // MATCHA_SPLIT_COMMA=0 keeps comma clauses inside the sentence (the
          // comma token itself renders as a pause): fewer sentences = fewer
          // per-sentence ab/wav fixed costs, ~half the ab time on long lines.
          static const std::pair<const char *, const char *> PUNCTS[] = {
              {"。", "."}, {"！", "!"}, {"？", "?"}, {"；", ";"}, {"，", ","},
          };
          // MATCHA_SPLIT_COMMA=1 restores per-clause splitting (slower speech,
          // better RTF optics — production default is OFF: the comma token
          // itself renders as a natural pause)
          const bool split_comma =
              getenv("MATCHA_SPLIT_COMMA") != nullptr && atoi(getenv("MATCHA_SPLIT_COMMA")) != 0;
          bool is_punct = false;
          for (const auto &pp : PUNCTS) {
            if (t.compare(i, 3, pp.first) == 0) {
              auto it = tok2id.find(pp.second);
              if (it != tok2id.end()) cur.push_back(it->second);
              const bool hard = !(pp.first == std::string("，") && !split_comma);
              if (!cur.empty() && hard) { sents.push_back(cur); cur.clear(); }
              i += 3;
              is_punct = true;
              break;
            }
          }
          if (!is_punct) ++i;  // skip ASCII/unknown byte
        }
      }
      if (!cur.empty()) sents.push_back(cur);
      // AddBlank interleave: [pad, t0, pad, t1, ..., pad]
      const int64_t pad = tok2id.count("_") ? tok2id.at("_") : 1;
      for (auto &s : sents) {
        std::vector<int64_t> b(s.size() * 2 + 1, pad);
        for (size_t j = 0; j < s.size(); ++j) b[1 + j * 2] = s[j];
        s = std::move(b);
      }
    }
    printf("[matcha] %zu sentences\n", sents.size());
    if (sents.empty()) {
      // LLM 回复可能带 emoji/纯符号段（实录 😊 段 G2P 为空）。静默跳过：
      // 不写 DONE（player 找不到文件自然跳过）、不算失败，守护循环继续
      printf("[matcha] skip: empty/_symbol-only text\n");
      return true;
    }
    std::vector<float> audio_out;
    std::vector<float> ort_out;
    double t_aco = 0, t_kpu = 0, t_ab = 0, t_estk = 0;
    for (size_t si = 0; si < sents.size(); ++si) {
      const auto &ids = sents[si];
      const int64_t L = (int64_t)ids.size();
      int64_t shp[2] = {1, L};
      int64_t one[1] = {1};
      int64_t xl = L;
      const char *in[] = {"x", "x_length", "noise_scale", "length_scale"};
      std::vector<Ort::Value> vi;
      vi.push_back(Ort::Value::CreateTensor<int64_t>(mem, const_cast<int64_t *>(ids.data()), ids.size(), shp, 2));
      vi.push_back(Ort::Value::CreateTensor<int64_t>(mem, &xl, 1, one, 1));
      vi.push_back(Ort::Value::CreateTensor<float>(mem, &ns, 1, one, 1));
      vi.push_back(Ort::Value::CreateTensor<float>(mem, &ls, 1, one, 1));
      auto a = clk::now();
      std::vector<float> mel_full;   // [80 * Lm] channel-major
      int64_t Lm = 0;
      if (est_mode) {
        // ===== A+B: KPU (matcha_A + CPU one-hot) or CPU (matcha_ab.onnx)
        // ===== + est128 KPU: 3 Euler steps per 128-frame window =====
        const float *mm = nullptr;      // [Lp,80] t-major expanded mu
        const float *msk = nullptr;     // [1,1,Lp]
        std::vector<float> mm_own, msk_own;
        int64_t Lp = 0;
        // kA is compiled for a fixed token bucket; longer sentences fall
        // back to the dynamic ab.onnx path when it is loaded.
        // TB=64 = shipped default bucket (A64 kmodel).
        const int64_t TB = getenv("MATCHA_TB") ? atoi(getenv("MATCHA_TB")) : 64;
        if (kA && L <= TB) {
          const int64_t XB = TB;
          std::vector<int64_t> x_b(XB, 1);
          memcpy(x_b.data(), ids.data(), L * sizeof(int64_t));
          int64_t xlA = L;             // ACTUAL token count (pads masked)
          float lsA = 1.0f;
          std::vector<float> dur(kA->output_bytes(0) / 4);   // [1,256]
          std::vector<float> hid(kA->output_bytes(1) / 4);   // [1,80,256]
          auto a2 = clk::now();
          kA->Run({x_b.data(), &xlA, &lsA}, {dur.data(), hid.data()});
          t_ab += ms(a2, clk::now());
          const int Ln = (int)L;
          std::vector<int> lo(Ln), hi(Ln);
          float acc = 0;
          for (int i2 = 0; i2 < Ln; ++i2) {
            lo[i2] = (int)acc;
            acc += dur[i2];
            hi[i2] = (int)acc;
          }
          if (hi[Ln - 1] < (int)acc) hi[Ln - 1] = (int)acc;
          Lp = hi[Ln - 1];
          mm_own.assign(Lp * 80, 0.0f);
          msk_own.assign(Lp, 1.0f);
          for (int n_i = 0; n_i < Ln; ++n_i)
            for (int t = lo[n_i]; t < hi[n_i] && t < (int)Lp; ++t)
              for (int ch = 0; ch < 80; ++ch)
                mm_own[t * 80 + ch] = hid[ch * XB + n_i];
          mm = mm_own.data();
          msk = msk_own.data();
        } else {
          if (!aco) {
            fprintf(stderr, "[matcha] L=%d exceeds MATCHA_TB=%d and no --matcha-acoustic fallback\n",
                    (int)L, (int)TB);
            return false;
          }
          const char *ab_out[] = {"/MatMul_output_0", "/Cast_3_output_0"};
          auto a2 = clk::now();
          auto out = aco->Run(Ort::RunOptions{nullptr}, in, vi.data(), 4, ab_out, 2);
          t_ab += ms(a2, clk::now());
          Lp = out[0].GetTensorTypeAndShapeInfo().GetShape()[1];
          // MUST copy: `out` dies at this block's end (use-after-free corrupted
          // mm/msk nondeterministically before)
          const float *src0 = out[0].GetTensorData<float>();
          const float *src1 = out[1].GetTensorData<float>();
          mm_own.assign(src0, src0 + Lp * 80);
          msk_own.assign(src1, src1 + Lp);
          mm = mm_own.data();
          msk = msk_own.data();
        }
        mel_full.assign(80 * Lp, 0.0f);
        if (getenv("MATCHA_DUMP_MM")) {
          char p[256];
          snprintf(p, sizeof(p), "%s.mm.f32", m_out.c_str());
          FILE *f = fopen(p, "ab");
          if (f) { fwrite(mm, 4, Lp * 80, f); fwrite(msk, 4, Lp, f); fclose(f); }
        }
        std::mt19937 rng(0xC0FFEEu + (uint32_t)si);
        std::normal_distribution<float> g(0.f, ns);
        // window width must match the compiled est kmodel's static frame dim
        const int WB = getenv("MATCHA_WIN") ? atoi(getenv("MATCHA_WIN")) : 128;
        // back-aligned windows: every window with Lp>WB is FULL of real frames
        // (padded tail windows corrupt real frames: GN group stats + attention
        // pad keys; measured cos 0.83 tiled vs 0.999 back-aligned)
        std::vector<int64_t> starts;
        {
          int64_t c0 = 0;
          while (c0 + WB < Lp) {
            starts.push_back(c0);
            c0 += WB;
          }
          starts.push_back(std::max<int64_t>(0, Lp - WB));
        }
        std::vector<float> x(80 * WB), muW(80 * WB), v(80 * WB), mk(WB);
        int win_idx = 0;
        for (int64_t c0 : starts) {
          const int f = (int)std::min<int64_t>(WB, Lp - c0);
          std::fill(mk.begin(), mk.end(), 0.0f);
          for (int t = 0; t < f; ++t) {
            mk[t] = msk[c0 + t];
            for (int ch = 0; ch < 80; ++ch)
              muW[ch * WB + t] = mm[(c0 + t) * 80 + ch];
          }
          for (auto &e : x) e = g(rng);
          if (getenv("MATCHA_DUMP_WIN")) {
            char p[256];
            snprintf(p, sizeof(p), "%s.win%zu_%d", m_out.c_str(), si, win_idx);
            FILE *wf = fopen(p, "wb");
            if (wf) {
              fwrite(muW.data(), 4, muW.size(), wf);
              fwrite(mk.data(), 4, mk.size(), wf);
              fwrite(x.data(), 4, x.size(), wf);
              fclose(wf);
            }
          }
          auto a3 = clk::now();
          // Euler steps over t in [0,1); 2 = shipped default (listening+ASR
          // gated vs 3-step on board, 2026-09-12)
          const int ODE = getenv("MATCHA_ODE_STEPS") ? atoi(getenv("MATCHA_ODE_STEPS")) : 2;
          for (int k = 0; k < ODE; ++k) {
            float t = (float)k / ODE;
            kEst->Run({x.data(), mk.data(), muW.data(), &t}, {v.data()});
            for (int i = 0; i < 80 * WB; ++i) x[i] += v[i] / ODE;
          }
          t_estk += ms(a3, clk::now());
          if (getenv("MATCHA_DUMP_WIN")) {
            char p[256];
            snprintf(p, sizeof(p), "%s.winmel%zu_%d", m_out.c_str(), si, win_idx);
            FILE *wf = fopen(p, "wb");
            if (wf) { fwrite(x.data(), 4, x.size(), wf); fclose(wf); }
          }
          win_idx++;
          for (int ch = 0; ch < 80; ++ch)
            for (int t = 0; t < f; ++t)
              mel_full[ch * Lp + c0 + t] = x[ch * WB + t] * 2.7628188f - 5.9870973f;
        }
        Lm = Lp;
        t_aco += ms(a, clk::now());
      } else if (abc_mode) {
        // ===== A (KPU) + B (CPU one-hot) + C128 windows (KPU) =====
        const int64_t XB = 256, WB = 128;
        std::vector<int64_t> x_b(XB, 1);
        memcpy(x_b.data(), ids.data(), L * sizeof(int64_t));
        int64_t xl256 = L;              // ACTUAL token count (pad must be masked!)
        float ls1 = 1.0f;
        std::vector<float> dur(kA->output_bytes(0) / sizeof(float));   // [1,256]
        std::vector<float> hid(kA->output_bytes(1) / sizeof(float));   // [1,80,256]
        kA->Run({x_b.data(), &xl256, &ls1}, {dur.data(), hid.data()});
        // one-hot: h_up[t] = hid[:, token(t)]
        const int Ln = (int)L;
        std::vector<int> lo(Ln), hi(Ln);
        {
          // float cumsum (same as the graph's internal length regulator);
          // boundaries use ceil to match the graph's Range+Less construction
          float acc = 0;
          for (int i2 = 0; i2 < Ln; ++i2) {
            lo[i2] = (int)acc;
            acc += dur[i2];
            hi[i2] = (int)acc;
          }
          if (hi[Ln - 1] < (int)acc) hi[Ln - 1] = (int)acc;  // ceil for last
        }
        // total length = ceil of the raw float sum (matches ReduceSum->Ceil),
        // then round UP to a multiple of 4 (U-Net stride-2 down/up needs even
        // lengths at every level; odd totals break the skip-connection Concat)
        float dsum = 0;
        for (int i2 = 0; i2 < Ln; ++i2) dsum += dur[i2];
        int64_t Lp = (int64_t)std::ceil(dsum);
        if (Lp % 4 != 0) Lp = ((Lp / 4) + 1) * 4;
        hi[Ln - 1] = (int)Lp;  // extend last token to cover the pad
        mel_full.assign(80 * Lp, 0.0f);
        if (kC) {
          // full-KPU: C in 128-frame windows
          const int64_t WB = 128;
          std::vector<float> mm(kC->input_bytes(0) / sizeof(float));
          std::vector<float> mk(kC->input_bytes(1) / sizeof(float));
          std::vector<float> du(kC->input_bytes(2) / sizeof(float));
          memcpy(du.data(), dur.data(), XB * sizeof(float));
          std::vector<float> mwin(kC->output_bytes(0) / sizeof(float));
          for (int64_t c0 = 0; c0 < Lp; c0 += WB) {
            const int64_t f = std::min<int64_t>(WB, Lp - c0);
            std::fill(mm.begin(), mm.end(), 0.0f);
            std::fill(mk.begin(), mk.end(), 0.0f);
            for (int n_i = 0; n_i < Ln; ++n_i) {
              const int a0 = std::max<int64_t>(lo[n_i] - c0, 0);
              const int b0 = std::min<int64_t>(hi[n_i] - c0, f);
              for (int t = a0; t < b0; ++t)
                for (int ch = 0; ch < 80; ++ch)
                  mm[t * 80 + ch] = hid[ch * XB + n_i];
            }
            for (int t = 0; t < f; ++t) mk[t] = 1.0f;
            kC->Run({mm.data(), mk.data(), du.data()}, {mwin.data()});
            for (int ch = 0; ch < 80; ++ch)
              memcpy(&mel_full[ch * Lp + c0], &mwin[ch * WB], f * sizeof(float));
          }
        } else if (cOrt) {
          // hybrid: A on KPU + C on CPU ORT (Cext, verified bit-exact).
          // Single full-length call; Cext also needs x/x_length/length_scale.
          std::vector<float> mm(Lp * 80);
          std::vector<float> mask(Lp, 1.0f);
          for (int n_i = 0; n_i < Ln; ++n_i)
            for (int t = lo[n_i]; t < hi[n_i] && t < Lp; ++t)
              for (int ch = 0; ch < 80; ++ch)
                mm[t * 80 + ch] = hid[ch * XB + n_i];
          float nsf = ns, lsf = 1.0f;
          int64_t xl = L;
          int64_t one[1] = {1};
          int64_t mm_shp[3] = {1, Lp, 80};
          int64_t mk_shp[3] = {1, 1, Lp};
          int64_t x_shp[2] = {1, XB};
          const char *cins[] = {"/MatMul_output_0", "/Cast_3_output_0",
                                "noise_scale", "x", "x_length", "length_scale"};
          const char *couts[] = {"mel"};
          std::vector<Ort::Value> cv;
          cv.push_back(Ort::Value::CreateTensor<float>(mem, mm.data(), mm.size(), mm_shp, 3));
          cv.push_back(Ort::Value::CreateTensor<float>(mem, mask.data(), mask.size(), mk_shp, 3));
          cv.push_back(Ort::Value::CreateTensor<float>(mem, &nsf, 1, one, 1));
          cv.push_back(Ort::Value::CreateTensor<int64_t>(mem, x_b.data(), XB, x_shp, 2));
          cv.push_back(Ort::Value::CreateTensor<int64_t>(mem, &xl, 1, one, 1));
          cv.push_back(Ort::Value::CreateTensor<float>(mem, &lsf, 1, one, 1));
          auto cout = cOrt->Run(Ort::RunOptions{nullptr}, cins, cv.data(), 6, couts, 1);
          const float *mo = cout[0].GetTensorData<float>();
          memcpy(mel_full.data(), mo, 80 * Lp * sizeof(float));
        }
        Lm = Lp;
        t_aco += ms(a, clk::now());
      } else {
        const char *aco_out[] = {"mel"};
        auto out = aco->Run(Ort::RunOptions{nullptr}, in, vi.data(), 4, aco_out, 1);
        t_aco += ms(a, clk::now());
        auto info = out[0].GetTensorTypeAndShapeInfo();
        Lm = info.GetShape()[2];
        const float *mp0 = out[0].GetTensorData<float>();
        mel_full.assign(mp0, mp0 + 80 * Lm);
      }
      const float *mp = mel_full.data();
      if (getenv("MATCHA_DUMP_MEL")) {
        char p[256];
        snprintf(p, sizeof(p), "%s.mel.f32", m_out.c_str());
        FILE *f = fopen(p, "ab");
        if (f) { fwrite(mp, 4, 80 * Lm, f); fclose(f); }
      }
      std::vector<float> wav;
      if (voc_is_ort) {
        // ORT vocoder on the exact-length mel (quality reference path)
        int64_t v_shape[3] = {1, 80, Lm};
        Ort::Value mv = Ort::Value::CreateTensor<float>(
            mem, const_cast<float *>(mp), 80 * Lm, v_shape, 3);
        const char *vin[] = {"mel"};
        const char *vout[] = {"audio"};
        auto b = clk::now();
        auto vres = ovoc->Run(Ort::RunOptions{nullptr}, vin, &mv, 1, vout, 1);
        t_kpu += ms(b, clk::now());
        size_t n_el = vres[0].GetTensorTypeAndShapeInfo().GetElementCount();
        const float *vp = vres[0].GetTensorData<float>();
        wav.assign(vp, vp + n_el);
      } else {
        // KPU vocoder in fixed-frame chunks (the b512 bucket's tiled
        // ConvTranspose kernels are broken on K230; single-tile b128 is
        // verified cos 0.96). Chunks crossfaded by 1 frame (256 samples).
        const int64_t CH = getenv("MATCHA_VOC_CHUNK") ? atoi(getenv("MATCHA_VOC_CHUNK")) : 128;
        const size_t XF = 256;  // crossfade samples (1 hop)
        std::vector<float> chunk(kvoc->output_bytes(0) / sizeof(float));
        int64_t produced = 0;
        for (int64_t c0 = 0; c0 < Lm; c0 += CH) {
          const int64_t f = std::min<int64_t>(CH, Lm - c0);
          std::vector<float> mel_pad(80 * CH, -12.0f);
          for (int cb = 0; cb < 80; ++cb)
            memcpy(&mel_pad[cb * CH], &mp[cb * Lm + c0], f * sizeof(float));
          if (!eq_c.empty())
            for (int b = 0; b < 80; ++b)
              for (int t = 0; t < CH; ++t)
                mel_pad[b * CH + t] = (mel_pad[b * CH + t] - eq_c[b]) * eq_s[b];
          auto b = clk::now();
          kvoc->Run({mel_pad.data()}, {chunk.data()});
          t_kpu += ms(b, clk::now());
          const size_t n = (size_t)f * 256;
          if (produced == 0) {
            wav.assign(chunk.begin(), chunk.begin() + n);
          } else {
            // crossfade the new chunk's head with the previous tail
            for (size_t j = 0; j < XF && j < wav.size() && j < n; ++j) {
              const double w = (double)j / XF;
              wav[wav.size() - XF + j] =
                  (float)(wav[wav.size() - XF + j] * (1.0 - w) + chunk[j] * w);
            }
            wav.insert(wav.end(), chunk.begin() + XF, chunk.begin() + n);
          }
          produced += f;
        }
      }
      const size_t n = wav.size();
      // 20ms fade-out at the sentence tail (last chunk's boundary artifact)
      const size_t fade = std::min<size_t>(n, 441);
      for (size_t j = 0; j < fade; ++j) wav[n - fade + j] *= (float)(1.0 - (double)j / fade);
      if (!voc_is_ort) {
        // notch out the hifigan KPU quantization tone at exactly sr/4
        // (5512.5 Hz @ 22050; measured peak/band ~21x, present since f1)
        // RBJ notch w0=pi/2, Q=25: y = b0*(x[n]+x[n-2]) - a2*y[n-2]
        const float alpha = 1.0f / (2.0f * 25.0f);
        const float b0 = 1.0f / (1.0f + alpha);
        const float a2 = (1.0f - alpha) / (1.0f + alpha);
        float xm2 = 0, ym2 = 0, xm1 = wav.empty() ? 0 : wav[0], ym1 = 0;
        for (size_t j = 0; j < n; ++j) {
          const float y = b0 * (wav[j] + xm2) - a2 * ym2;
          xm2 = xm1; xm1 = wav[j];
          ym2 = ym1; ym1 = y;
          wav[j] = y;
        }
      }
      audio_out.insert(audio_out.end(), wav.begin(), wav.begin() + n);
      if (voc_dual && ovoc) {
        // same-mel ORT reference for A/B: exact-length vocoding
        int64_t v_shape[3] = {1, 80, Lm};
        Ort::Value mv = Ort::Value::CreateTensor<float>(
            mem, const_cast<float *>(mp), 80 * Lm, v_shape, 3);
        const char *vin[] = {"mel"};
        const char *vout[] = {"audio"};
        auto vres = ovoc->Run(Ort::RunOptions{nullptr}, vin, &mv, 1, vout, 1);
        size_t n_el = vres[0].GetTensorTypeAndShapeInfo().GetElementCount();
        const float *vp = vres[0].GetTensorData<float>();
        ort_out.insert(ort_out.end(), vp, vp + n_el);
      }
      printf("  [matcha] sent %zu: %ld tok -> mel %ld -> %.2fs\n",
             si, (long)L, (long)Lm, n / 22050.0);
    }
    {
      auto a = clk::now();
      sherpa_onnx::WriteWave(m_out, 22050, audio_out.data(), audio_out.size());
      if (!ort_out.empty()) {
        std::string ortp = m_out + ".ort.wav";
        sherpa_onnx::WriteWave(ortp, 22050, ort_out.data(), ort_out.size());
        printf("[matcha] ort ref wav: %s\n", ortp.c_str());
      }
      printf("[matcha] wav write %.1fms\n", ms(a, clk::now()));
    }
    const double audio_s = (double)audio_out.size() / 22050.0;
    if (est_mode)
      printf("[matcha] DONE %s audio=%.2fs aco=%.0fms (ab=%.0fms estk=%.0fms) kpu=%.0fms RTF=%.3f\n",
             m_out.c_str(), audio_s, t_aco, t_ab, t_estk, t_kpu,
             (t_aco + t_kpu) / 1000.0 / audio_s);
    else
      printf("[matcha] DONE %s audio=%.2fs aco=%.0fms kpu=%.0fms RTF=%.3f\n",
             m_out.c_str(), audio_s, t_aco, t_kpu, (t_aco + t_kpu) / 1000.0 / audio_s);
    if (daemon_mode) {
      // svc/chatd protocol line — same format as the piper daemon
      printf("DONE %s %.2f %.3f\n", m_out.c_str(), audio_s,
             audio_s > 0 ? (t_aco + t_kpu) / 1000.0 / audio_s : 9.999);
      fflush(stdout);
    }
    return true;
    };

    if (daemon_mode) {
      printf("READY\n");
      fflush(stdout);
      std::string dline;
      while (std::getline(std::cin, dline)) {
        if (dline.empty() || dline[0] == '#') continue;
        if (dline == "QUIT") break;
        const size_t q1 = dline.find('\t');
        const size_t q2 =
            (q1 == std::string::npos) ? std::string::npos : dline.find('\t', q1 + 1);
        const size_t q3 =
            (q2 == std::string::npos) ? std::string::npos : dline.find('\t', q2 + 1);
        if (q3 == std::string::npos) {
          printf("ERROR protocol: expected <wav>\t<sid>\t<speed>\t<text>\n");
          fflush(stdout);
          continue;
        }
        const std::string wav_req = dline.substr(0, q1);
        const std::string spd_req = dline.substr(q2 + 1, q3 - q2 - 1);
        try {
          float spd = std::stof(spd_req);
          ls = (spd > 0) ? (1.0f / spd) : 1.0f;
        } catch (...) { ls = 1.0f; }
        if (!synth_matcha(dline.substr(q3 + 1), wav_req))
          printf("ERROR synth %s\n", wav_req.c_str());
        fflush(stdout);
      }
      mmz_shim_release_all();
      return 0;
    }
    if (!synth_matcha(matcha_say, out_wav)) return 1;
    mmz_shim_release_all();
    _exit(0);
  }

  if (!probe_matcha.empty()) {
    if (matcha_acoustic.empty() || matcha_vocoder.empty() || matcha_kmodel.empty()) {
      fprintf(stderr, "--probe-matcha needs --matcha-acoustic/vocoder/kmodel\n");
      return 1;
    }
    using clk = std::chrono::steady_clock;
    auto ms = [](clk::time_point a, clk::time_point b) {
      return std::chrono::duration<double, std::milli>(b - a).count();
    };
    k230_kpu_init();
    Ort::Env menv(ORT_LOGGING_LEVEL_WARNING, "matcha");
    Ort::SessionOptions mso;
    mso.SetIntraOpNumThreads(1);
    mso.SetInterOpNumThreads(1);
    auto mem = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);
    auto t0 = clk::now();
    Ort::Session aco(menv, matcha_acoustic.c_str(), mso);
    printf("[matcha] acoustic load %.0fms\n", ms(t0, clk::now()));
    t0 = clk::now();
    Ort::Session voc(menv, matcha_vocoder.c_str(), mso);
    printf("[matcha] vocoder(ort) load %.0fms\n", ms(t0, clk::now()));
    t0 = clk::now();
    KpuModel kvoc(matcha_kmodel, "hifigan");
    printf("[matcha] vocoder(kpu) load %.0fms\n", ms(t0, clk::now()));
    // optional per-band mel equalization (folded-vocoder input, see
    // mk_hifigan_eq.py): mel_eq.bin = 160 f32 (80 offsets + 80 scales)
    // next to the kmodel
    std::vector<float> eq_c, eq_s;
    {
      const std::string dir = matcha_kmodel.substr(0, matcha_kmodel.find_last_of('/'));
      FILE *f = fopen((dir + "/mel_eq.bin").c_str(), "rb");
      if (f) {
        std::vector<float> buf(160);
        if (fread(buf.data(), 4, 160, f) == 160) {
          eq_c.assign(buf.begin(), buf.begin() + 80);
          eq_s.assign(buf.begin() + 80, buf.end());
        }
        fclose(f);
      }
      printf("[matcha] mel_eq: %s\n", eq_c.empty() ? "none" : "loaded");
    }

    auto toks = ReadBinary<int64_t>(probe_matcha);
    const int64_t L = static_cast<int64_t>(toks.size());
    printf("[matcha] tokens=%ld\n", (long)L);
    int64_t x_shape[2] = {1, L};
    int64_t one_shape[1] = {1};
    int64_t xl = L;
    float ns = 0.667f, ls = 1.0f;
    if (getenv("MATCHA_NS")) ns = atof(getenv("MATCHA_NS"));  // sherpa matcha default is 1.0
    std::vector<float> mel;
    int64_t Lm = 0;
    double aco_ms = 0;
    if (probe_matcha.size() > 4 &&
        probe_matcha.compare(probe_matcha.size() - 4, 4, ".bin") == 0 &&
        probe_matcha.find("mel") != std::string::npos && getenv("PROBE_MEL_RAW")) {
      // raw mel probe: feed a saved mel directly (frame count from the file,
      // no padding) — the KPU vocoder consumes it as-is (layout [80][frames])
      auto mf = ReadBinary<float>(probe_matcha);
      mel.assign(mf.begin(), mf.end());
      Lm = static_cast<int64_t>(mf.size() / 80);
      printf("[matcha] PROBE_MEL_RAW: %zu floats -> %ld frames\n", mf.size(), (long)Lm);
    } else
    for (int r = 0; r < 2; ++r) {
      Ort::Value xv = Ort::Value::CreateTensor<int64_t>(mem, toks.data(), toks.size(), x_shape, 2);
      Ort::Value xlv = Ort::Value::CreateTensor<int64_t>(mem, &xl, 1, one_shape, 1);
      Ort::Value nsv = Ort::Value::CreateTensor<float>(mem, &ns, 1, one_shape, 1);
      Ort::Value lsv = Ort::Value::CreateTensor<float>(mem, &ls, 1, one_shape, 1);
      const char *in[] = {"x", "x_length", "noise_scale", "length_scale"};
      std::vector<Ort::Value> aco_ins;
      aco_ins.reserve(4);
      aco_ins.push_back(std::move(xv));
      aco_ins.push_back(std::move(xlv));
      aco_ins.push_back(std::move(nsv));
      aco_ins.push_back(std::move(lsv));
      auto a = clk::now();
      const char *aco_out[] = {"mel"};
      auto out = aco.Run(Ort::RunOptions{nullptr}, in, aco_ins.data(), 4, aco_out, 1);
      if (r == 1) aco_ms = ms(a, clk::now());
      auto info = out[0].GetTensorTypeAndShapeInfo();
      size_t n_el = info.GetElementCount();
      const float *p = out[0].GetTensorData<float>();
      mel.assign(p, p + n_el);
      Lm = info.GetShape()[2];
    }
    printf("[matcha] acoustic %.1fms -> mel[1,80,%ld]\n", aco_ms, (long)Lm);
    if (Lm > 512) {
      printf("[matcha] mel L=%ld > 512 bucket: probe truncated\n", (long)Lm);
    }
    const int64_t Lc = std::min<int64_t>(Lm, 512);

    // ORT vocoder on the exact-length mel (reference)
    std::vector<float> wav_ref;
    double voc_ort_ms = 0;
    for (int r = 0; r < 2; ++r) {
      int64_t m_shape[3] = {1, 80, Lc};
      Ort::Value mv = Ort::Value::CreateTensor<float>(mem, mel.data(), 80 * Lc, m_shape, 3);
      const char *in[] = {"mel"};
      auto a = clk::now();
      const char *voc_out[] = {"audio"};
      auto out = voc.Run(Ort::RunOptions{nullptr}, in, &mv, 1, voc_out, 1);
      if (r == 1) voc_ort_ms = ms(a, clk::now());
      size_t n_el = out[0].GetTensorTypeAndShapeInfo().GetElementCount();
      const float *p = out[0].GetTensorData<float>();
      if (r == 1) wav_ref.assign(p, p + n_el);
    }

    // KPU vocoder input: raw-mel mode feeds the file as-is; otherwise pad
    // into the 512 bucket with the log-mel silence floor (0 = loudest!)
    std::vector<float> mel_pad;
    if (getenv("PROBE_MEL_RAW")) {
      mel_pad = mel;
    } else {
      mel_pad.assign(80 * 512, -12.0f);
      for (int c = 0; c < 80; ++c)
        memcpy(&mel_pad[c * 512], &mel[c * Lm], Lc * sizeof(float));
      if (!eq_c.empty())
        for (int b = 0; b < 80; ++b)
          for (int t = 0; t < 512; ++t)
            mel_pad[b * 512 + t] = (mel_pad[b * 512 + t] - eq_c[b]) * eq_s[b];
    }
    std::vector<float> wav_kpu(kvoc.output_bytes(0) / sizeof(float));
    double voc_kpu_ms = 0, voc_kpu_cpu = 0;
    for (int r = 0; r < 2; ++r) {
      auto a = clk::now();
      clock_t c0 = std::clock();
      kvoc.Run({mel_pad.data()}, {wav_kpu.data()});
      if (r == 1) {
        voc_kpu_ms = ms(a, clk::now());
        voc_kpu_cpu = 1000.0 * (std::clock() - c0) / CLOCKS_PER_SEC;
      }
    }

    const size_t n_cmp = static_cast<size_t>(Lc) * 256;
    // dump raw outputs for offline cross-correlation analysis
    {
      FILE *f = fopen("/tmp/mref.f32", "wb");
      if (f) { fwrite(wav_ref.data(), 4, std::min(wav_ref.size(), n_cmp + 8192), f); fclose(f); }
      f = fopen("/tmp/mkpu.f32", "wb");
      if (f) { fwrite(wav_kpu.data(), 4, wav_kpu.size(), f); fclose(f); }
      f = fopen("/tmp/mmel.f32", "wb");
      if (f) { fwrite(mel_pad.data(), 4, mel_pad.size(), f); fclose(f); }
    }
    double dot = 0, na = 0, nb = 0, mx = 0;
    for (size_t i = 0; i < n_cmp; ++i) {
      const double a = wav_ref[i], b = wav_kpu[i];
      dot += a * b; na += a * a; nb += b * b;
      mx = std::max(mx, std::fabs(a - b));
    }
    const double cos = dot / (std::sqrt(na * nb) + 1e-30);
    const double audio_s = static_cast<double>(n_cmp) / 22050.0;
    printf("[matcha] vocoder ort %.1fms | kpu %.1fms (cpu %.1fms)\n",
           voc_ort_ms, voc_kpu_ms, voc_kpu_cpu);
    printf("[matcha] cos=%.6f maxdiff=%.4f over %zu samples (%.2fs audio)\n",
           cos, mx, n_cmp, audio_s);
    printf("[matcha] RTF: acoustic=%.3f voc_ort=%.3f voc_kpu=%.3f total(kpu)=%.3f\n",
           aco_ms / 1000.0 / audio_s, voc_ort_ms / 1000.0 / audio_s,
           voc_kpu_ms / 1000.0 / audio_s,
           (aco_ms + voc_kpu_ms) / 1000.0 / audio_s);
    mmz_shim_release_all();
    _exit(0);
  }
  if (daemon_mode) {
    printf("Mode: daemon (resident worker)\n");
  } else {
    printf("Text: %s\n", text.c_str());
    printf("SID: %ld, Speed: %.2f\n", (long)sid, speed);
    printf("Output WAV: %s\n", out_wav.c_str());
  }

  auto t_start = std::chrono::steady_clock::now();
  auto wall_ms = [](std::chrono::steady_clock::time_point a,
                    std::chrono::steady_clock::time_point b) {
    return std::chrono::duration<double, std::milli>(b - a).count();
  };
  struct StageTimes {
    double norm = 0, lex_load = 0, tok = 0, sess_load = 0;
    double enc_w = 0, enc_c = 0, dp_w = 0, dp_c = 0, lr = 0, kpu_w = 0, kpu_c = 0, wav = 0;
  } st;

  // ==== one-time init ====
  // Text normalizers (rule FSTs) — construct once, Normalize() per utterance
  std::vector<std::string> fst_files;
  {
    std::stringstream ss(rule_fsts);
    std::string item;
    while (std::getline(ss, item, ',')) {
      if (!item.empty()) fst_files.push_back(item);
    }
  }
  std::vector<std::unique_ptr<kaldifst::TextNormalizer>> normalizers;
  for (const auto &fst : fst_files)
    normalizers.push_back(std::make_unique<kaldifst::TextNormalizer>(fst));

  // Lexicon (one-time)
  std::unique_ptr<sherpa_onnx::Lexicon> lexicon;
  {
    auto a = std::chrono::steady_clock::now();
    lexicon = std::make_unique<sherpa_onnx::Lexicon>(lexicon_path, tokens_path, "", "Chinese", false);
    st.lex_load += wall_ms(a, std::chrono::steady_clock::now());
  }
  // polyphone words to wrap: one word per line from word_wrap.txt next to the
  // lexicon; missing file = no wrapping
  std::vector<std::string> wrap_words;
  {
    const std::string dir = lexicon_path.substr(0, lexicon_path.find_last_of('/'));
    std::ifstream wf(dir + "/word_wrap.txt");
    std::string w;
    while (std::getline(wf, w)) {
      while (!w.empty() && (w.back() == '\r' || w.back() == '\n')) w.pop_back();
      if (!w.empty()) wrap_words.push_back(w);
    }
    printf("[wrap] %zu polyphone words\n", wrap_words.size());
  }
  // 3. Initialize ORT & CPU Encoder (session load is one-time cost)
  Ort::Env env(ORT_LOGGING_LEVEL_WARNING, "kpu_vits");
  Ort::SessionOptions sess_opts;
  sess_opts.SetIntraOpNumThreads(1);
  sess_opts.SetInterOpNumThreads(1);
  sess_opts.SetGraphOptimizationLevel(GraphOptimizationLevel::ORT_ENABLE_ALL);
  {
    auto providers = Ort::GetAvailableProviders();
    printf("[enc] available providers:");
    for (const auto &p : providers) printf(" %s", p.c_str());
    printf("\n");
  }
  if (use_shl) {
    OrtStatus *st = Ort::GetApi().OrtSessionOptionsAppendExecutionProvider_Shl(
        sess_opts, "");
    if (st != nullptr) {
      fprintf(stderr, "[enc] SHL EP append failed: %s (falling back to CPU)\n",
              Ort::GetApi().GetErrorMessage(st));
      Ort::GetApi().ReleaseStatus(st);
    } else {
      printf("[enc] SHL EP appended (RVV kernels for conv/gemm)\n");
    }
  }
  std::unique_ptr<Ort::Session> enc_sess;
  if (enc_kmodel.empty() || compare_enc) {
    auto a = std::chrono::steady_clock::now();
    enc_sess = std::make_unique<Ort::Session>(env, enc_onnx.c_str(), sess_opts);
    st.sess_load += wall_ms(a, std::chrono::steady_clock::now());
  }
  std::unique_ptr<Ort::Session> dp_sess;  // lazy: hybrid path or logw dump
  auto mem_info = Ort::MemoryInfo::CreateCpu(OrtDeviceAllocator, OrtMemTypeDefault);

  const float noise_scale = 0.667f;
  float alpha = (speed > 0) ? (1.0f / speed) : 1.0f;
  float noise_scale_dur = 0.8f;
  if (nsd_override >= 0.0f) noise_scale_dur = nsd_override;
  std::mt19937 rng(12345);
  std::normal_distribution<float> eps_dist(0.0f, 1.0f);

  // 4. Initialize KPU models (one-time load)
  k230_kpu_init();
  KpuModel kpu(subgen_kmodel, "subgen");
  const size_t frame_size = kpu.input_bytes(0) / sizeof(float) / 96;
  const size_t samples_per_frame = kpu.output_bytes(0) / sizeof(float) / frame_size;
  std::unique_ptr<KpuModel> enc_kpu;
  size_t enc_bucket = 0;
  if (!enc_kmodel.empty()) {
    enc_kpu = std::make_unique<KpuModel>(enc_kmodel, "enc");
    enc_bucket = enc_kpu->input_bytes(0) / sizeof(int64_t);
    printf("[enc] KPU bucket N=%zu\n", enc_bucket);
    auto a = std::chrono::steady_clock::now();
    dp_sess = std::make_unique<Ort::Session>(env, dp_onnx.c_str(), sess_opts);
    st.sess_load += wall_ms(a, std::chrono::steady_clock::now());
  }
  std::unique_ptr<Ort::Session> enc_only_sess;  // enc-only (mu+logs) for the dp-KPU combo
  std::unique_ptr<KpuModel> dp_kpu;
  size_t dp_bucket = 0;
  bool dp_dpx_style = false;  // kmodel takes dp_x (f32 [1,96,bucket]) as input 0
  if (!dp_kmodel.empty()) {
    dp_kpu = std::make_unique<KpuModel>(dp_kmodel, "dp");
    dp_bucket = dp_kpu->input_bytes(0) / sizeof(int64_t);
    dp_dpx_style = (dp_kpu->input_bytes(0) == 96 * sizeof(float) *
                       (dp_kpu->input_bytes(1) / sizeof(int64_t)));
    if (dp_dpx_style) {
      printf("[dp] KPU dp_x-style (enc excluded), bucket N=%zu\n",
             dp_kpu->input_bytes(1) / sizeof(int64_t));
      dp_bucket = dp_kpu->input_bytes(1) / sizeof(int64_t);
      if (!enc_kmodel.empty()) {
        fprintf(stderr, "[dp] dp_x kmodel needs the f32 CPU enc (--enc-only-onnx); "
                        "--enc-kmodel provides no dp_x\n");
        return 1;
      }
    } else {
      printf("[dp] KPU bucket N=%zu (enc inside, int8)\n", dp_bucket);
    }
    if (enc_kmodel.empty()) {
      auto a = std::chrono::steady_clock::now();
      enc_only_sess = std::make_unique<Ort::Session>(env, enc_only_onnx.c_str(), sess_opts);
      st.sess_load += wall_ms(a, std::chrono::steady_clock::now());
    }
    if (!dump_logw.empty()) {
      // ORT reference for the logw dump (static bucket, deterministic at nsd=0)
      auto a = std::chrono::steady_clock::now();
      dp_sess = std::make_unique<Ort::Session>(env, dp_onnx.c_str(), sess_opts);
      st.sess_load += wall_ms(a, std::chrono::steady_clock::now());
    }
  }
  // bucketed mu snapshot for the logw dump (filled by whichever enc path runs)
  std::vector<float> mu64_dbg;
  // dp entry features [1,96,bucket], zero-padded from the dynamic enc output
  std::vector<float> dpx64;
  const size_t dp_bucket_max = 64;

  // --probe-dpx=<x.bin>,<tok.bin>,<tl.bin>,<spk.bin>: raw-file kmodel probe
  // (x: 96*64 f32, tok: 64 i64, tl/spk: 1 i64). Prints logw and exits.
  if (!probe_dpx.empty()) {
    std::vector<std::string> parts;
    {
      std::stringstream ss(probe_dpx);
      std::string item;
      while (std::getline(ss, item, ','))
        if (!item.empty()) parts.push_back(item);
    }
    if (parts.size() != 4 || !dp_kpu) {
      fprintf(stderr, "--probe-dpx needs x.bin,tok.bin,tl.bin,spk.bin + --dp-kmodel\n");
      return 1;
    }
    auto xf = ReadBinary<float>(parts[0]);
    auto tokf = ReadBinary<int64_t>(parts[1]);
    auto tlf = ReadBinary<int64_t>(parts[2]);
    auto spkf = ReadBinary<int64_t>(parts[3]);
    if (xf.size() != 96 * 64 || tokf.size() != 64 || tlf.size() != 1 || spkf.size() != 1) {
      fprintf(stderr, "probe size mismatch: x=%zu tok=%zu tl=%zu spk=%zu\n",
              xf.size(), tokf.size(), tlf.size(), spkf.size());
      return 1;
    }
    float nsd = noise_scale_dur, al = alpha;
    std::vector<float> out(dp_kpu->output_bytes(0) / sizeof(float));
    dp_kpu->Run({xf.data(), tokf.data(), tlf.data(), spkf.data(), &nsd, &al}, {out.data()});
    printf("PROBE");
    for (float v : out) printf(" %.5f", v);
    printf("\n");
    mmz_shim_release_all();
    _exit(0);
  }

  const char *enc_in_names[] = {"tokens", "tokens_lens", "speaker",
                                "noise_scale", "alpha", "noise_scale_dur"};
  const char *enc_out_names[] = {"/text_encoder/Split_output_0",
                                 "/text_encoder/Split_output_1",
                                 "/Mul_1_output_0"};

  auto run_enc = [&](const std::vector<int64_t> &tokens,
                     std::vector<float> &mu_out,
                     std::vector<float> &logs_out,
                     std::vector<float> &logw_out) {
    const int64_t n = static_cast<int64_t>(tokens.size());
    int64_t tok_shape[2] = {1, n};
    int64_t one_shape[1] = {1};
    int64_t tok_len = n, spk_id = sid;
    float ns = noise_scale, al = alpha, nsd = noise_scale_dur;
    Ort::Value tok_val = Ort::Value::CreateTensor<int64_t>(
        mem_info, const_cast<int64_t *>(tokens.data()), tokens.size(), tok_shape, 2);
    Ort::Value tl_val = Ort::Value::CreateTensor<int64_t>(mem_info, &tok_len, 1, one_shape, 1);
    Ort::Value spk_val = Ort::Value::CreateTensor<int64_t>(mem_info, &spk_id, 1, one_shape, 1);
    Ort::Value ns_val = Ort::Value::CreateTensor<float>(mem_info, &ns, 1, one_shape, 1);
    Ort::Value alpha_val = Ort::Value::CreateTensor<float>(mem_info, &al, 1, one_shape, 1);
    Ort::Value nsd_val = Ort::Value::CreateTensor<float>(mem_info, &nsd, 1, one_shape, 1);
    std::vector<Ort::Value> ins;
    ins.push_back(std::move(tok_val));
    ins.push_back(std::move(tl_val));
    ins.push_back(std::move(spk_val));
    ins.push_back(std::move(ns_val));
    ins.push_back(std::move(alpha_val));
    ins.push_back(std::move(nsd_val));
    auto outs = enc_sess->Run(Ort::RunOptions{nullptr}, enc_in_names, ins.data(),
                              ins.size(), enc_out_names, 3);
    auto copy_flat = [](const Ort::Value &v) {
      auto info = v.GetTensorTypeAndShapeInfo();
      size_t n_el = info.GetElementCount();
      const float *p = v.GetTensorData<float>();
      return std::vector<float>(p, p + n_el);
    };
    mu_out = copy_flat(outs[0]);
    logs_out = copy_flat(outs[1]);
    logw_out = copy_flat(outs[2]);
  };

  auto run_enc_only = [&](const std::vector<int64_t> &tokens,
                          std::vector<float> &mu_out,
                          std::vector<float> &logs_out,
                          std::vector<float> &dpx_out) {
    const int64_t n = static_cast<int64_t>(tokens.size());
    int64_t tok_shape[2] = {1, n};
    int64_t one_shape[1] = {1};
    int64_t tok_len = n, spk_id = sid;
    float ns = noise_scale, al = alpha, nsd = noise_scale_dur;
    Ort::Value tok_val = Ort::Value::CreateTensor<int64_t>(
        mem_info, const_cast<int64_t *>(tokens.data()), tokens.size(), tok_shape, 2);
    Ort::Value tl_val = Ort::Value::CreateTensor<int64_t>(mem_info, &tok_len, 1, one_shape, 1);
    Ort::Value spk_val = Ort::Value::CreateTensor<int64_t>(mem_info, &spk_id, 1, one_shape, 1);
    std::vector<Ort::Value> ins;
    ins.push_back(std::move(tok_val));
    ins.push_back(std::move(tl_val));
    ins.push_back(std::move(spk_val));
    // enc_dp.onnx (combined enc+dp) takes 6 inputs; we only request the Split
    // outputs + the dp entry features, so ORT prunes the dp subgraph — the
    // extra scalars are dummies.
    const char *in3[] = {"tokens", "tokens_lens", "speaker"};
    const char *in6[] = {"tokens", "tokens_lens", "speaker",
                         "noise_scale", "alpha", "noise_scale_dur"};
    const char **in_names = in3;
    if (enc_only_sess->GetInputCount() >= 6) {
      Ort::Value ns_val = Ort::Value::CreateTensor<float>(mem_info, &ns, 1, one_shape, 1);
      Ort::Value alpha_val = Ort::Value::CreateTensor<float>(mem_info, &al, 1, one_shape, 1);
      Ort::Value nsd_val = Ort::Value::CreateTensor<float>(mem_info, &nsd, 1, one_shape, 1);
      ins.push_back(std::move(ns_val));
      ins.push_back(std::move(alpha_val));
      ins.push_back(std::move(nsd_val));
      in_names = in6;
    }
    // mu, logs, and the duration-predictor entry features (after_norm transposed)
    const char *out_names[] = {"/text_encoder/Split_output_0",
                               "/text_encoder/Split_output_1",
                               "/text_encoder/Transpose_output_0"};
    auto outs = enc_only_sess->Run(Ort::RunOptions{nullptr}, in_names, ins.data(),
                                   ins.size(), out_names, 3);
    auto copy_flat = [](const Ort::Value &v) {
      auto info = v.GetTensorTypeAndShapeInfo();
      size_t n_el = info.GetElementCount();
      const float *p = v.GetTensorData<float>();
      return std::vector<float>(p, p + n_el);
    };
    mu_out = copy_flat(outs[0]);
    logs_out = copy_flat(outs[1]);
    // dp_x arrives as [1,96,L]; zero-pad each channel row into the 64 bucket
    // (flow convs are masked, pad values never leak — verified 0-flip on the
    // 20-sentence corpus)
    dpx_out.assign(96 * dp_bucket_max, 0.0f);
    const float *px = outs[2].GetTensorData<float>();
    for (int c = 0; c < 96; ++c)
      memcpy(&dpx_out[c * dp_bucket_max], &px[c * n], n * sizeof(float));
  };

  auto run_dp = [&](const std::vector<float> &mu_in,
                    const std::vector<int64_t> &tokens,
                    std::vector<float> &logw_out) {
    if (!dp_sess) {
      fprintf(stderr, "dp session not loaded\n");
      exit(1);
    }
    const int64_t L = static_cast<int64_t>(tokens.size());
    int64_t mu_shape[3] = {1, 96, L};
    int64_t tok_shape[2] = {1, L};
    int64_t one_shape[1] = {1};
    int64_t tok_len = L, spk_id = sid;
    float nsd = noise_scale_dur, al = alpha;
    Ort::Value mu_val = Ort::Value::CreateTensor<float>(
        mem_info, const_cast<float *>(mu_in.data()), mu_in.size(), mu_shape, 3);
    Ort::Value tok_val = Ort::Value::CreateTensor<int64_t>(
        mem_info, const_cast<int64_t *>(tokens.data()), tokens.size(), tok_shape, 2);
    Ort::Value tl_val = Ort::Value::CreateTensor<int64_t>(mem_info, &tok_len, 1, one_shape, 1);
    Ort::Value spk_val = Ort::Value::CreateTensor<int64_t>(mem_info, &spk_id, 1, one_shape, 1);
    Ort::Value nsd_val = Ort::Value::CreateTensor<float>(mem_info, &nsd, 1, one_shape, 1);
    Ort::Value alpha_val = Ort::Value::CreateTensor<float>(mem_info, &al, 1, one_shape, 1);
    const char *in_names[] = {"/text_encoder/Split_output_0", "tokens", "tokens_lens",
                              "speaker", "noise_scale_dur", "alpha"};
    const char *out_names[] = {"/Mul_1_output_0"};
    std::vector<Ort::Value> ins;
    ins.push_back(std::move(mu_val));
    ins.push_back(std::move(tok_val));
    ins.push_back(std::move(tl_val));
    ins.push_back(std::move(spk_val));
    ins.push_back(std::move(nsd_val));
    ins.push_back(std::move(alpha_val));
    auto outs = dp_sess->Run(Ort::RunOptions{nullptr}, in_names, ins.data(),
                             ins.size(), out_names, 1);
    auto info = outs[0].GetTensorTypeAndShapeInfo();
    size_t n_el = info.GetElementCount();
    const float *p = outs[0].GetTensorData<float>();
    logw_out.assign(p, p + n_el);
  };


  // ==== per-utterance driver ====
  // single-shot: one pass with argv text. --daemon: one utterance per stdin
  // line. The per-utterance body below keeps its original 2-space indent.
  if (daemon_mode) printf("READY\n");
  int utterance = 0;
  for (;;) {
    if (daemon_mode) {
      std::string line;
      if (!std::getline(std::cin, line)) break;
      if (line == "QUIT") break;
      if (line.empty() || line.rfind("//", 0) == 0) continue;
      const size_t p1 = line.find('\t');
      const size_t p2 = (p1 == std::string::npos) ? std::string::npos : line.find('\t', p1 + 1);
      const size_t p3 = (p2 == std::string::npos) ? std::string::npos : line.find('\t', p2 + 1);
      if (p3 == std::string::npos) {
        printf("ERROR protocol: expected <wav>\t<sid>\t<speed>\t<text>\n");
        continue;
      }
      const std::string wav_req = line.substr(0, p1);
      const std::string sid_req = line.substr(p1 + 1, p2 - p1 - 1);
      const std::string spd_req = line.substr(p2 + 1, p3 - p2 - 1);
      text = line.substr(p3 + 1);
      try { sid = std::stoll(sid_req); } catch (...) { sid = 10; }
      float spd = 1.0f;
      try { spd = std::stof(spd_req); } catch (...) {}
      alpha = (spd > 0) ? (1.0f / spd) : 1.0f;
      if (!wav_req.empty()) out_wav = wav_req;
      rng.seed(12345);  // per-utterance determinism == old per-process behavior
    } else if (utterance > 0) {
      break;
    }
    ++utterance;
    st.norm = st.tok = st.enc_w = st.enc_c = st.dp_w = st.dp_c = 0;
    st.lr = st.kpu_w = st.kpu_c = st.wav = 0;
    const auto t_utt = std::chrono::steady_clock::now();
    try {
      // normalize (per-utterance)
      {
        auto a = std::chrono::steady_clock::now();
        for (const auto &tn : normalizers) text = tn->Normalize(text);
        st.norm += wall_ms(a, std::chrono::steady_clock::now());
      }
      // polyphone word wrapping: words listed in word_wrap.txt get #$|w|$#
      // markers — the lexicon's Chinese path merges such spans into one word
      // and looks up its multi-char entry (e.g. 音乐 → yīn yuè instead of the
      // single-char default 乐=lè). The board's heteronym fst is a 127-byte
      // stub, so this table is the working disambiguation mechanism.
      for (const auto &w : wrap_words) {
        const std::string marked = "#$|" + w + "|$#";
        size_t p = 0;
        while ((p = text.find(w, p)) != std::string::npos) {
          text.replace(p, w.size(), marked);
          p += marked.size();
        }
      }
      printf("Normalized text: %s\n", text.c_str());

      // tokenize (per-utterance)
      std::vector<sherpa_onnx::TokenIDs> token_ids;
      {
        auto a = std::chrono::steady_clock::now();
        token_ids = lexicon->ConvertTextToTokenIds(text);
        if (getenv("TTS_DUMP_TOKENS")) {
          printf("[tokens] sent=%zu ids:", token_ids.size());
          for (auto id : token_ids[0].tokens) printf(" %d", (int)id);
          printf("\n");
        }
        st.tok += wall_ms(a, std::chrono::steady_clock::now());
      }
      if (token_ids.empty() || token_ids[0].tokens.empty()) {
        if (daemon_mode) {
          printf("ERROR tokenize: no tokens for text\n");
          continue;
        }
        fprintf(stderr, "Error: Failed to convert text to tokens\n");
        return 1;
      }
      printf("Sentences: %zu\n", token_ids.size());
  std::vector<float> audio_out;
  int sent_idx = 0;
  for (const auto &sent : token_ids) {
    const std::vector<int64_t> tokens = sent.tokens;
    if (tokens.empty()) continue;
    const int num_tokens = static_cast<int>(tokens.size());
    printf("[Sentence %d] tokens=%d\n", sent_idx, num_tokens);

    std::vector<float> mu, logs, logw;
    bool used_kpu_enc = false;
    if (enc_kpu && static_cast<size_t>(num_tokens) <= enc_bucket) {
      // KPU text_encoder: pad tokens into the bucket, run, slice to L
      std::vector<int64_t> tok_b(enc_bucket, 0);
      memcpy(tok_b.data(), tokens.data(), num_tokens * sizeof(int64_t));
      int64_t lens = num_tokens;
      std::vector<float> mu64(enc_kpu->output_bytes(0) / sizeof(float));
      std::vector<float> logs64(enc_kpu->output_bytes(1) / sizeof(float));
      {
        auto a = std::chrono::steady_clock::now();
        clock_t c0 = std::clock();
        enc_kpu->Run({tok_b.data(), &lens, &sid}, {mu64.data(), logs64.data()});
        st.enc_c += 1000.0 * (std::clock() - c0) / CLOCKS_PER_SEC;
        st.enc_w += wall_ms(a, std::chrono::steady_clock::now());
      }
      mu.resize(96 * num_tokens);
      logs.resize(96 * num_tokens);
      for (int c = 0; c < 96; ++c) {
        memcpy(&mu[c * num_tokens], &mu64[c * enc_bucket], num_tokens * sizeof(float));
        memcpy(&logs[c * num_tokens], &logs64[c * enc_bucket], num_tokens * sizeof(float));
      }
      mu64_dbg = mu64;  // bucketed KPU-enc mu for the logw dump
      if (compare_enc) {
        std::vector<float> mu_c, logs_c, logw_c;
        run_enc(tokens, mu_c, logs_c, logw_c);
        double dot = 0, na = 0, nb = 0, mx = 0;
        for (int c = 0; c < 96; ++c)
          for (int i = 0; i < num_tokens; ++i) {
            const double a = mu[c * num_tokens + i], b = mu_c[c * num_tokens + i];
            dot += a * b; na += a * a; nb += b * b;
            mx = std::max(mx, std::fabs(a - b));
          }
        printf("  [enc-compare] L=%d mu cos=%.6f maxdiff=%.4f\n",
               num_tokens, dot / std::sqrt(na * nb + 1e-30), mx);
      }
      if (!(dp_kpu && static_cast<size_t>(num_tokens) <= dp_bucket)) {
        auto a = std::chrono::steady_clock::now();
        clock_t c0 = std::clock();
        // dp session is the STATIC bucket: feed padded mu/tokens, output is
        // logw[1,1,bucket] with pads zeroed -> keep the first L values
        run_dp(mu64, tok_b, logw);
        logw.resize(num_tokens);
        st.dp_c += 1000.0 * (std::clock() - c0) / CLOCKS_PER_SEC;
        st.dp_w += wall_ms(a, std::chrono::steady_clock::now());
      }
      used_kpu_enc = true;
    } else {
      if (enc_kpu)
        printf("  [enc] L=%d > bucket %zu -> CPU fallback\n", num_tokens, enc_bucket);
      auto a = std::chrono::steady_clock::now();
      clock_t c0 = std::clock();
      if (dp_kpu && static_cast<size_t>(num_tokens) <= dp_bucket)
        run_enc_only(tokens, mu, logs, dpx64);
      else
        run_enc(tokens, mu, logs, logw);
      st.enc_c += 1000.0 * (std::clock() - c0) / CLOCKS_PER_SEC;
      st.enc_w += wall_ms(a, std::chrono::steady_clock::now());
      if (!dump_logw.empty() && dp_kpu && static_cast<size_t>(num_tokens) <= dp_bucket) {
        // gold CPU mu padded into the dp bucket for the logw dump
        mu64_dbg.assign(96 * dp_bucket, 0.0f);
        for (int c = 0; c < 96; ++c)
          memcpy(&mu64_dbg[c * dp_bucket], &mu[c * num_tokens], num_tokens * sizeof(float));
      }
      if (bench && sent_idx == 0) {
        for (int r = 1; r <= 2; ++r) {
          auto b = std::chrono::steady_clock::now();
          std::vector<float> m2, l2, w2;
          run_enc(tokens, m2, l2, w2);
          printf("  [enc rerun %d] %.1f ms\n", r, wall_ms(b, std::chrono::steady_clock::now()));
        }
      }
    }
    if (dp_kpu && static_cast<size_t>(num_tokens) <= dp_bucket) {
      // KPU duration predictor (canvas bucket): pad tokens to dp_bucket, run,
      // keep the first L values of logw[1,1,bucket]
      std::vector<int64_t> tok_d(dp_bucket, 0);
      memcpy(tok_d.data(), tokens.data(), num_tokens * sizeof(int64_t));
      int64_t lens = num_tokens;
      float nsd = noise_scale_dur, al = alpha;
      std::vector<float> logw64(dp_kpu->output_bytes(0) / sizeof(float));
      double dp_kpu_ms = 0;
      {
        auto a = std::chrono::steady_clock::now();
        clock_t c0 = std::clock();
        if (dp_dpx_style)
          dp_kpu->Run({dpx64.data(), tok_d.data(), &lens, &sid, &nsd, &al}, {logw64.data()});
        else
          dp_kpu->Run({tok_d.data(), &lens, &sid, &nsd, &al}, {logw64.data()});
        st.dp_c += 1000.0 * (std::clock() - c0) / CLOCKS_PER_SEC;
        st.dp_w += wall_ms(a, std::chrono::steady_clock::now());
        dp_kpu_ms = wall_ms(a, std::chrono::steady_clock::now());
      }
      logw.assign(logw64.begin(), logw64.begin() + num_tokens);
      printf("  [dp-kpu] %d tokens -> %.1fms\n", num_tokens, dp_kpu_ms);
      if (!dump_logw.empty() && dp_sess &&
          (!mu64_dbg.empty() || !dpx64.empty())) {
        std::vector<float> logw_ort;
        // dp_b64.onnx recomputes enc internally (f32); its mu input is
        // vestigial, any [1,96,bucket] tensor satisfies the signature.
        run_dp(dp_dpx_style ? dpx64 : mu64_dbg, tok_d, logw_ort);
        FILE *f = fopen(dump_logw.c_str(), "a");
        if (f) {
          fprintf(f, "sent %d L %d nsd %.3f sid %ld\n", sent_idx, num_tokens,
                  noise_scale_dur, (long)sid);
          fprintf(f, "kmodel:");
          for (int i = 0; i < num_tokens; ++i) fprintf(f, " %.5f", logw[i]);
          fprintf(f, "\nort   :");
          for (int i = 0; i < num_tokens; ++i) fprintf(f, " %.5f", logw_ort[i]);
          fprintf(f, "\n");
          fclose(f);
        }
      }
    }
    if (enc_kpu && used_kpu_enc && bench && sent_idx == 0) {
      for (int r = 1; r <= 2; ++r) {
        std::vector<int64_t> tok_b(enc_bucket, 0);
        memcpy(tok_b.data(), tokens.data(), num_tokens * sizeof(int64_t));
        int64_t lens = num_tokens;
        std::vector<float> mu64(enc_kpu->output_bytes(0) / sizeof(float));
        std::vector<float> logs64(enc_kpu->output_bytes(1) / sizeof(float));
        auto b = std::chrono::steady_clock::now();
        enc_kpu->Run({tok_b.data(), &lens, &sid}, {mu64.data(), logs64.data()});
        printf("  [enc-kpu rerun %d] %.1f ms\n", r, wall_ms(b, std::chrono::steady_clock::now()));
      }
    }

    // 4. Length Regulator: /Mul_1_output_0 is already exp(w)*alpha, so ceil() only
    std::vector<int> durs(num_tokens);
    int total_frames = 0;
    for (int i = 0; i < num_tokens; ++i) {
      durs[i] = static_cast<int>(std::ceil(logw[i]));
      if (durs[i] < 1) durs[i] = 1;
      total_frames += durs[i];
    }

    // z = mu_rep + noise_scale * eps * exp(logs_rep), ONNX layout [96, total]
    auto a_lr = std::chrono::steady_clock::now();
    std::vector<float> z(96 * total_frames);
    int cur_frame = 0;
    for (int i = 0; i < num_tokens; ++i) {
      const int d = durs[i];
      for (int c = 0; c < 96; ++c) {
        const float m = mu[c * num_tokens + i];
        const float s = std::exp(logs[c * num_tokens + i]);
        float *dst = &z[c * total_frames + cur_frame];
        std::fill_n(dst, d, m);
        for (int t = 0; t < d; ++t)
          dst[t] += noise_scale * eps_dist(rng) * s;
      }
      cur_frame += d;
    }
    st.lr += wall_ms(a_lr, std::chrono::steady_clock::now());
    printf("  frames=%d audio=%.2fs\n", total_frames,
           static_cast<float>(total_frames) * samples_per_frame / 8000.0f);

    // 5. Chunked KPU Inference
    std::vector<float> z_chunk(96 * frame_size, 0.0f);
    std::vector<uint8_t> m_chunk(frame_size, 0);
    std::vector<float> chunk_audio(kpu.output_bytes(0) / sizeof(float), 0.0f);
    for (int c0 = 0; c0 < total_frames; c0 += frame_size) {
      const int f = std::min<int>(frame_size, total_frames - c0);
      std::fill(z_chunk.begin(), z_chunk.end(), 0.0f);
      std::fill(m_chunk.begin(), m_chunk.end(), 0);
      for (int c = 0; c < 96; ++c)
        memcpy(&z_chunk[c * frame_size], &z[c * total_frames + c0], f * sizeof(float));
      std::fill_n(m_chunk.begin(), f, 1);

      auto a = std::chrono::steady_clock::now();
      clock_t c0c = std::clock();
      kpu.Run({z_chunk.data(), m_chunk.data(), &sid}, {chunk_audio.data()});
      st.kpu_c += 1000.0 * (std::clock() - c0c) / CLOCKS_PER_SEC;
      st.kpu_w += wall_ms(a, std::chrono::steady_clock::now());
      printf("  [KPU chunk] %d frames wall=%.1fms\n", f, wall_ms(a, std::chrono::steady_clock::now()));
      audio_out.insert(audio_out.end(), chunk_audio.begin(),
                       chunk_audio.begin() + f * samples_per_frame);
    }
    ++sent_idx;
  }
  // 6. Write Audio WAV (8kHz, 16-bit mono)
  {
    auto a = std::chrono::steady_clock::now();
    sherpa_onnx::WriteWave(out_wav, 8000, audio_out.data(), audio_out.size());
    st.wav += wall_ms(a, std::chrono::steady_clock::now());
  }

      const double total_ms = wall_ms(t_utt, std::chrono::steady_clock::now());
      const float audio_sec = static_cast<float>(audio_out.size()) / 8000.0f;
      const double steady_ms = st.norm + st.tok + st.enc_w + st.dp_w + st.lr + st.kpu_w + st.wav;
      const double steady_cpu_ms = st.enc_c + st.dp_c + st.kpu_c;
      const double rtf = audio_sec > 0 ? (steady_ms / 1000.0) / audio_sec : 0.0;

      printf("\n=== Stage Budget (per utterance) ===\n");
      printf("norm      %7.1f ms\n", st.norm);
      printf("lexicon   %7.1f ms (one-time)\n", st.lex_load);
      printf("tokenize  %7.1f ms\n", st.tok);
      printf("ort load  %7.1f ms (one-time)\n", st.sess_load);
      printf("enc       %7.1f ms wall / %.1f ms cpu (%s)\n", st.enc_w, st.enc_c,
             enc_kmodel.empty() ? "cpu" : "kpu");
      printf("dp        %7.1f ms wall / %.1f ms cpu (cpu)\n", st.dp_w, st.dp_c);
      printf("len-reg   %7.1f ms\n", st.lr);
      printf("kpu gen   %7.1f ms wall / %.1f ms cpu\n", st.kpu_w, st.kpu_c);
      printf("wav write %7.1f ms\n", st.wav);
      printf("total     %7.1f ms wall (steady-state %.1f ms)\n", total_ms, steady_ms);
      printf("audio     %.2f s\n", audio_sec);
      printf("RTF       %.3f (steady, target < 1.0)\n", rtf);
      printf("CPU/audio %7.1f ms/s (decode-budget impact)\n",
             audio_sec > 0 ? steady_cpu_ms / audio_sec : 0.0);
      printf("====================================\n");
      if (daemon_mode) printf("DONE %s %.2f %.3f\n", out_wav.c_str(), audio_sec, rtf);
    } catch (...) {
      if (!daemon_mode) throw;
      printf("ERROR exception during synthesis\n");
      continue;
    }
  }

  mmz_shim_release_all();
  _exit(0);
}