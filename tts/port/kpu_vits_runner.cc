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
#include <vector>
#include <unistd.h>

#include <nncase/runtime/interpreter.h>
#include <nncase/runtime/runtime_op_utility.h>
#include <nncase/runtime/runtime_tensor.h>
#include <nncase/runtime/simple_types.h>
#include <nncase/runtime/util.h>
#include <onnxruntime_cxx_api.h>

#include "kaldifst/csrc/text-normalizer.h"
#include "sherpa-onnx/csrc/lexicon.h"
#include "sherpa-onnx/csrc/wave-writer.h"
#include "k230_init.h"
#include "mmz.h"

extern "C" void mmz_shim_release_all(void);
extern "C" int kd_mpi_sys_mmz_flush_cache(uint64_t phy_addr, void *virt_addr, uint32_t len);
extern "C" uint64_t mmz_cpu_to_phys(uint64_t v);

using namespace nncase::runtime;

class KpuModel {
 public:
  explicit KpuModel(const std::string &kmodel_path, const char *tag) {
    std::ifstream ifs(kmodel_path, std::ios::binary);
    if (!ifs.is_open()) {
      fprintf(stderr, "[KPU:%s] Error: Failed to open %s\n", tag, kmodel_path.c_str());
      exit(1);
    }
    interp_.load_model(ifs).expect("[KPU] Invalid kmodel");

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

  size_t input_bytes(size_t i) const { return input_bytes_[i]; }
  size_t output_bytes(size_t i) const { return output_bytes_[i]; }

  // byte sizes derived from the kmodel IO descriptors
  void Run(const std::vector<const void *> &in, const std::vector<void *> &out) {
    for (size_t i = 0; i < in.size(); ++i) {
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
  if (argc >= 2 && std::string(argv[1]) == "--daemon") {
    daemon_mode = true;
  } else if (argc < 2) {
    fprintf(stderr,
            "Usage: %s <text> [options]\n"
            "       %s --daemon [options]   (resident worker, reads stdin)\n"
            "Options:\n"
            "  --subgen-kmodel=<path> (default: /mnt/data/aishell3/subgen.kmodel)\n"
            "  --enc-onnx=<path>      (default: /mnt/data/aishell3/enc_dp.onnx)\n"
            "  --enc-only-onnx=<path> (default: /mnt/data/aishell3/enc.onnx; mu+logs only,\n"
            "                         used for the CPU-enc + --dp-kmodel combo)\n"
            "  --dp-kmodel=<path>     (duration predictor on KPU; empty = ORT dp)\n"
            "  --lexicon=<path>       (default: /mnt/data/aishell3/lexicon.txt)\n"
            "  --tokens=<path>        (default: /mnt/data/aishell3/tokens.txt)\n"
            "  --rule-fsts=<path>     (default: /mnt/data/aishell3/phone.fst,/mnt/data/aishell3/date.fst,/mnt/data/aishell3/number.fst)\n"
            "  --sid=<int>            (default: 10)\n"
            "  --speed=<float>        (default: 1.0)\n"
            "  --output=<path>        (default: /mnt/data/aishell3/tts_kpu_out.wav)\n"
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
  std::string subgen_kmodel = "/mnt/data/aishell3/subgen.kmodel";
  std::string enc_onnx = "/mnt/data/aishell3/enc_dp.onnx";
  std::string enc_only_onnx = "/mnt/data/aishell3/enc.onnx";
  std::string lexicon_path = "/mnt/data/aishell3/lexicon.txt";
  std::string tokens_path = "/mnt/data/aishell3/tokens.txt";
  std::string rule_fsts = "/mnt/data/aishell3/phone.fst,/mnt/data/aishell3/date.fst,/mnt/data/aishell3/number.fst";
  int64_t sid = 10;
  float speed = 1.0f;
  bool bench = false;
  bool use_shl = false;
  std::string enc_kmodel;  // empty = text_encoder stays on CPU via enc_dp.onnx
  std::string dp_kmodel;   // empty = duration predictor via ORT
  std::string dp_onnx = "/mnt/data/aishell3/dp.onnx";
  std::string out_wav = "/mnt/data/aishell3/tts_kpu_out.wav";
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
  std::string matcha_lexicon;
  std::string matcha_tokens;

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
    else if (arg.rfind("--matcha-lexicon=", 0) == 0) matcha_lexicon = arg.substr(17);
    else if (arg.rfind("--matcha-tokens=", 0) == 0) matcha_tokens = arg.substr(16);
    else if (arg.rfind("--output=", 0) == 0) out_wav = arg.substr(9);
  }

  printf("=== KPU VITS TTS Runner ===\n");

  // ==== matcha probe: acoustic (CPU) + vocoder ORT vs KPU comparison ====
  // ==== matcha say: text -> (greedy longest-match lexicon) -> acoustic CPU
  // ==== -> KPU vocoder -> 22050Hz wav. Needs --matcha-acoustic/kmodel/lexicon.
  if (!matcha_say.empty()) {
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
    std::unique_ptr<KpuModel> kA, kC;
    std::unique_ptr<Ort::Session> cOrt;
    if (abc_mode) {
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
      const std::string &t = matcha_say;
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
          // punctuation: map to the ASCII token, append, and split the sentence
          static const std::pair<const char *, const char *> PUNCTS[] = {
              {"。", "."}, {"！", "!"}, {"？", "?"}, {"；", ";"}, {"，", ","},
          };
          bool is_punct = false;
          for (const auto &pp : PUNCTS) {
            if (t.compare(i, 3, pp.first) == 0) {
              auto it = tok2id.find(pp.second);
              if (it != tok2id.end()) cur.push_back(it->second);
              if (!cur.empty()) { sents.push_back(cur); cur.clear(); }
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

    float ns = 1.0f, ls = 1.0f;  // sherpa matcha defaults (NOT vits' 0.667!)
    if (getenv("MATCHA_NS")) ns = atof(getenv("MATCHA_NS"));
    std::vector<float> audio_out;
    std::vector<float> ort_out;
    double t_aco = 0, t_kpu = 0;
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
      if (abc_mode) {
        // ===== A (KPU) + B (CPU one-hot) + C128 windows (KPU) =====
        const int64_t XB = 256, WB = 128;
        std::vector<int64_t> x_b(XB, 1);
        memcpy(x_b.data(), ids.data(), L * sizeof(int64_t));
        int64_t xl256 = XB;             // A was calibrated with full-bucket lens
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
          // hybrid: A on KPU + C on CPU ORT (full length, single call)
          std::vector<float> mm(Lp * 80);
          for (int n_i = 0; n_i < Ln; ++n_i)
            for (int t = lo[n_i]; t < hi[n_i]; ++t)
              for (int ch = 0; ch < 80; ++ch)
                mm[t * 80 + ch] = hid[ch * XB + n_i];
          std::vector<float> mask(Lp, 1.0f);
          int64_t mm_shp[3] = {1, Lp, 80};
          int64_t mk_shp[3] = {1, 1, Lp};
          int64_t du_shp[3] = {1, 1, XB};
          int64_t one[1] = {1};
          float nsf = ns;
          const char *cins[] = {"/MatMul_output_0", "/Cast_3_output_0",
                                "/Mul_1_output_0", "noise_scale"};
          const char *couts[] = {"/decoder/Add_2_output_0"};
          std::vector<Ort::Value> cv;
          cv.push_back(Ort::Value::CreateTensor<float>(mem, mm.data(), mm.size(), mm_shp, 3));
          cv.push_back(Ort::Value::CreateTensor<float>(mem, mask.data(), mask.size(), mk_shp, 3));
          cv.push_back(Ort::Value::CreateTensor<float>(mem, dur.data(), XB, du_shp, 3));
          cv.push_back(Ort::Value::CreateTensor<float>(mem, &nsf, 1, one, 1));
          auto cout = cOrt->Run(Ort::RunOptions{nullptr}, cins, cv.data(), 4, couts, 1);
          const float *mo = cout[0].GetTensorData<float>();
          // C-dyn output is the ODE result — same layout [1,80,L']
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
        snprintf(p, sizeof(p), "%s.mel.f32", out_wav.c_str());
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
        // KPU vocoder in 128-frame chunks (the b512 bucket's tiled
        // ConvTranspose kernels are broken on K230; single-tile b128 is
        // verified cos 0.96). Chunks crossfaded by 1 frame (256 samples).
        const int64_t CH = 128;
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
      sherpa_onnx::WriteWave(out_wav, 22050, audio_out.data(), audio_out.size());
      if (!ort_out.empty()) {
        std::string ortp = out_wav + ".ort.wav";
        sherpa_onnx::WriteWave(ortp, 22050, ort_out.data(), ort_out.size());
        printf("[matcha] ort ref wav: %s\n", ortp.c_str());
      }
      printf("[matcha] wav write %.1fms\n", ms(a, clk::now()));
    }
    const double audio_s = (double)audio_out.size() / 22050.0;
    printf("[matcha] DONE %s audio=%.2fs aco=%.0fms kpu=%.0fms RTF=%.3f\n",
           out_wav.c_str(), audio_s, t_aco, t_kpu, (t_aco + t_kpu) / 1000.0 / audio_s);
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