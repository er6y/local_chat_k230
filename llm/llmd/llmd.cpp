// llmd — K230 常驻 LLM 服务 daemon（本项目 llama.cpp K230 fork）
// 模型 + KV cache 常驻，跨轮上下文连续；前缀复用只 decode 每轮新增段。
// 滑窗：上下文装不下时淘汰最老一轮，KV 中间删除 + 位置压实（seq_rm/seq_add），
// 前缀不断裂、不整窗重算。
//
// stdin 行协议（FIFO，写端需常驻 holder 防 EOF）：
//   <out_path>\t<n_predict>\t<用户输入>     生成文本流式写 out_path（think 块不落盘）
//   QUIT                                    退出
//   #... / 空行                             忽略
// stdout：READY（模型加载完）；DONE <out> <n_gen> <sec> <tps>；ERROR ...
//
// 环境变量（svc 脚本负责）：LD_BIND_NOW=1、KPU=1（等价 --kpu）、
// KPU_KMODEL_DIR=/mnt/data/kpu_qwen/s4sq_q8（S4 tile 自动选择，懒加载+LRU）

#include "llama.h"
#include "common.h"
#include "chat.h"

#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <fcntl.h>
#include <unistd.h>
#include <iostream>
#include <string>
#include <vector>
#include <chrono>
#include <csignal>
#include <algorithm>

using clk = std::chrono::steady_clock;

// ---- 流水 TTS：句子直送合成队列 ------------------------------------
// LLMD_TTS_FIFO 未设则整条路径关闭（兼容纯文本用法）。FIFO 用 O_NONBLOCK
// 打开：TTS 链没起时丢弃该句并告警，绝不阻塞 decode。行协议与 svc_tts 的
// runner 一致：<wav>\t<sid>\t<speed>\t<text>；播放在独立的 player 守护里
// 按 DONE 出现顺序进行（合成与解码重叠，播放按句序）。
// ---- TTS 文本规整（2026-09-14）--------------------------------------
// matcha 前端词库（lexicon_poly.txt 67910 条）零 ASCII 映射：数字和英文
// 字母送进去直接不出声。提示词约束对 0.6B 不可靠（实测照吐阿拉伯数字），
// 故在 TTS 副本上规整：数字→汉字、句中小数点→"点"、字母→普通话近音字。
// 只改送 TTS 的文本，屏幕/ask_out 保留模型原文。
static std::string tts_normalize(const std::string & in) {
    static const char * dig[] = {"零","一","二","三","四","五","六","七","八","九"};
    static const char * let[26] = {
        "诶","比","西","迪","衣","艾弗","吉","艾尺","艾","杰","开","艾勒",
        "艾马","恩","欧","批","吉吾","啊儿","艾丝","提","由","维","达布流",
        "艾克斯","歪","贼"};
    std::string o;
    o.reserve(in.size() * 3 + 8);
    size_t i = 0;
    while (i < in.size()) {
        unsigned char c = (unsigned char)in[i];
        if (c >= '0' && c <= '9') {
            o += dig[c - '0'];
            // 两侧都是数字的小数点读"点"："3.5"→三点五
            if (i + 2 < in.size() && in[i+1] == '.' &&
                in[i+2] >= '0' && in[i+2] <= '9') { o += "点"; i += 2; }
            i++;
        } else if (c >= 'a' && c <= 'z') {
            o += let[c - 'a']; i++;
        } else if (c >= 'A' && c <= 'Z') {
            o += let[c - 'A']; i++;
        } else {
            o += in[i]; i++;   // UTF-8 多字节按字节原样续上；'%/等符号 TTS 前端自然忽略
        }
    }
    return o;
}

static int g_tts_seq = 0;
static bool g_tts_mute = false;   // 预热轮（out 路径含 /warmup）：只暖 KV，不出声
static void tts_send_sentence(const std::string & s) {
    const char * fifo = getenv("LLMD_TTS_FIFO");
    if (!fifo || s.empty() || g_tts_mute) return;
    std::string line = tts_normalize(s);
    for (auto & c : line) if (c == '\n' || c == '\t' || c == '\r') c = ' ';
    while (line.compare(0, 3, "…") == 0) line.erase(0, 3);   // 省略号被切开后的残头
    if (line.find_first_not_of(' ') == std::string::npos) return;
    char wav[128], rec[2048];
    snprintf(wav, sizeof wav, "/tmp/chat/utt_%d_%03d.wav", (int)getpid(), ++g_tts_seq);
    int L = snprintf(rec, sizeof rec, "%s\t1\t1.0\t%s\n", wav, line.c_str());
    int fd = open(fifo, O_WRONLY | O_NONBLOCK);
    if (fd < 0) {
        fprintf(stderr, "[llmd] tts fifo unavailable, drop %d bytes sentence\n", L);
        return;
    }
    ssize_t w = write(fd, rec, (size_t)L);
    (void)w;
    close(fd);
}

// ---- 流水 TTS：按标点切段直送合成队列 ------------------------------
// 分隔符白名单（用户定版）：逗号、句号、问号、感叹号、分号、顿号、
// 省略号、换行。见一个就送一段，没有其它任何判断。
// 多字节安全：返回 s 中第一个分隔符“之后”的字节位（无则 npos）
static size_t idx_after_first_sep(const std::string & s) {
    const char * seps[] = {"，", "。", "？", "！", "；", "、", "……", "…", "\n"};
    size_t best = std::string::npos;
    for (const char * sp : seps) {
        size_t p = s.find(sp);
        if (p != std::string::npos) {
            size_t e = p + strlen(sp);
            if (best == std::string::npos || e < best) best = e;
        }
    }
    return best;
}

#if defined(__riscv)
// 与 cli.cpp 相同的 KPU 点亮方式：init 不会自动发生，必须显式调用
extern "C" void ggml_kpu_init(const char * dir);
static void kpu_maybe_init() {
    const char * kd = getenv("KPU_KMODEL_DIR");
    if (!kd) kd = "/mnt/data/kpu_qwen";
    ggml_kpu_init(kd);
}
#else
static void kpu_maybe_init() {}
#endif

struct TurnOut {
    FILE * f = nullptr;
    bool in_think = false;
    bool wrote_any = false;
    std::string pend;          // 尾部缓冲：可能截断的 <think>/</think> 前缀
    std::string think_open;    // "<think>"

    bool open(const char * path) {
        f = fopen(path, "wb");
        in_think = false;
        wrote_any = false;
        pend.clear();
        return f != nullptr;
    }
    // 过滤 <think>...</think> 再落盘；标签跨 token 也能兜住
    void write(const char * s, size_t n) {
        if (!f) return;
        pend.append(s, n);
        for (;;) {
            if (!in_think) {
                size_t p = pend.find("<think>");
                if (p != std::string::npos) {
                    if (p) { fwrite(pend.data(), 1, p, f); wrote_any = true; }
                    pend.erase(0, p + 7);
                    in_think = true;
                    continue;
                }
                size_t keep = std::min(pend.size(), (size_t)7);  // "<think>"
                if (pend.size() > keep) {
                    fwrite(pend.data(), 1, pend.size() - keep, f);
                    wrote_any = true;
                    pend.erase(0, pend.size() - keep);
                }
                return;
            } else {
                size_t p = pend.find("</think>");
                if (p != std::string::npos) {
                    pend.erase(0, p + 8);
                    in_think = false;
                    continue;
                }
                size_t keep = std::min(pend.size(), (size_t)8);  // "</think>"
                if (pend.size() > keep) pend.erase(0, pend.size() - keep);
                return;
            }
        }
    }
    void finish() {
        if (!f) return;
        // 兜底：整轮都闷在未闭合 think 块里（模型抽风）时，剥掉标签把
        // 正文念出来，绝不能静音——下游会把 <think> 之后全删掉
        if (!wrote_any && !pend.empty()) {
            size_t p;
            while ((p = pend.find("<think>")) != std::string::npos)
                pend.erase(p, 7);
            while ((p = pend.find("</think>")) != std::string::npos)
                pend.erase(p, 8);
            if (!pend.empty()) fwrite(pend.data(), 1, pend.size(), f);
            wrote_any = true;
        }
        else if (!in_think && !pend.empty()) fwrite(pend.data(), 1, pend.size(), f);
        pend.clear();
        fflush(f);
    }
    void close() { if (f) { finish(); fclose(f); f = nullptr; } }
};

struct Render {
    std::vector<llama_token> tok;
    std::string str;
};

static Render render_chat(
        common_chat_templates * tmpls,
        std::vector<common_chat_msg> msgs,   // by value：尾部再加当前 user 消息
        const std::string & user,
        const llama_vocab * vocab,
        bool add_user = true) {
    if (add_user) {
        common_chat_msg um;
        um.role = "user";
        um.content = user;
        msgs.push_back(std::move(um));
    }
    common_chat_templates_inputs in;
    in.messages = std::move(msgs);
    in.add_generation_prompt = true;
    in.use_jinja = true;
    in.enable_thinking = false;   // Qwen3：注入空 think 块，跳过思考直出答案
    common_chat_params p = common_chat_templates_apply(tmpls, in);
    Render r;
    r.str = std::move(p.prompt);
    r.tok = common_tokenize(vocab, r.str, false, true);
    return r;
}

// 渲染串去掉尾部生成提示（最后一段 <|im_start|>assistant ... 到结尾），
// 剩下的就是"历史消息的完整渲染"，作为增量拼接的基准前缀。
static std::string render_base(const std::string & s) {
    size_t gp = s.rfind("<|im_start|>assistant");
    return (gp == std::string::npos) ? s : s.substr(0, gp);
}

// SIGSEGV 现场取证（2026-09-13 KPU local 排查）：打印 C++ 调用链到 stderr，
// 配合 addr2line 用。常驻无副作用；-rdynamic 链接时符号名可读。
#include <execinfo.h>
static void llmd_segv_bt(int sig) {
    void * bt[48];
    int n = backtrace(bt, 48);
    fprintf(stderr, "\n[llmd] SIGNAL %d backtrace (%d frames):\n", sig, n);
    fflush(stderr);
    backtrace_symbols_fd(bt, n, 2);
    _exit(139);
}

int main(int argc, char ** argv) {
    signal(SIGSEGV, llmd_segv_bt);
    signal(SIGBUS, llmd_segv_bt);
    std::string model_path;
    // 精简版(38tok,省33% prefill)实测会让 0.6B 跑出 think 外泄+复读——
    // 指令密度是这个尺寸模型的"行为锚"，勿瘦（2026-09-10 A/B 定案）
    std::string system_prompt =
        "你是语音助手小K。用户说的\"我\"永远指用户本人，不是你。"
        "用户告诉你的名字和信息都要记住，回答要简短口语化。";
    int n_ctx = 2048, n_predict_def = 128;
    float temp = 0.7f, top_p = 0.8f;
    int top_k = 20;
    bool use_kpu = false;

    for (int i = 1; i < argc; i++) {
        std::string a = argv[i];
        auto next = [&](void) -> const char * { return (i + 1 < argc) ? argv[++i] : ""; };
        if (a == "--model") model_path = next();
        else if (a == "--ctx-size") n_ctx = atoi(next());
        else if (a == "--n-predict") n_predict_def = atoi(next());
        else if (a == "--temp") temp = atof(next());
        else if (a == "--top-p") top_p = atof(next());
        else if (a == "--top-k") top_k = atoi(next());
        else if (a == "--system") system_prompt = next();
        else if (a == "--kpu") use_kpu = true;
        else { fprintf(stderr, "[llmd] unknown arg %s\n", a.c_str()); return 1; }
    }
    if (model_path.empty()) { fprintf(stderr, "[llmd] --model required\n"); return 1; }

    // 2026-09-13：**禁用 ggml_backend_load_all**。llama-cli 从不调它（grep
    // 证实），而它会扫描目录 dlopen "libggml-*" 文件——板上目录里有多份
    // libggml-cpu 旧拷贝（.bak），同名不同 inode 的 dlopen = nncase 运行时
    // 第二实例，弱符号（vtable/typeinfo）跨副本错绑 → create() 对象头被踩
    // （llmd-local 必崩的头号嫌疑）。CPU-only 板上唯一后端是静态注册的 CPU
    // 后端，跳过无任何功能损失。
    // ggml_backend_load_all();
    // --kpu 点亮 KPU prefill 劫持。生产链路 = daemon 模式（KPU_DAEMON=1，
    // 客户端不持 kmodel、不占 CMA）+ KPU_HYBRID=1（decode M<32 留 CPU RVV，
    // prefill M>=32 上 KPU）。劫持失败已带 bool 返回 + CPU 回退，不会走
    // 未初始化的本地 nncase 路径；era-B S16 重导出后数值已过真激活验证
    if (use_kpu) kpu_maybe_init();
    // 消除手工 context_params 参数漂移，与 cli.cpp 同源初始化
    common_params params;
    params.model.path = model_path;
    params.n_ctx = n_ctx;
    params.n_batch = 1024;
    // ubatch 默认 512 会让 graph_reserve 出 ~309MB compute buffer，把 zone
    // 挤满后权重页缓存 fallback 进 CMA，占住 daemon 释放的 GNNE scratch 洞
    // （迁移不回 zone）→ serve 期 cma_alloc -12 → vendor 段错误。压到 64
    // 后 buffer ~50MB，484MB 权重缓存整个待在 zone，CMA 零借用（可用
    // LLMD_UBATCH 环境变量覆盖做 A/B）。
    params.n_ubatch = 64;
    if (const char * u = getenv("LLMD_UBATCH")) params.n_ubatch = atoi(u);
    // 权重加载模式（LLMD_LOAD_MODE=DIO|AUTO|NONE，默认 DIO）：
    // DIO（O_DIRECT 私有缓冲，零页缓存，峰值 ~456MB anon）——daemon 时代
    // 的定案（页缓存在 zone/CMA 间倒腾咬死 GNNE 连续分配）。
    // 2026-09-13 复评（local 模式 + KPU stems in CMA）：DIO 的 688MB 匿名
    // 权重把非 CMA 侧（~870MB 可用）塞满，内核被迫把 ~100MB 不可移动的
    // 匿名页钉进 CMA 区域——永久碎片化 + cma_alloc 冻结的机制根源。
    // AUTO=mmap 把权重还给可驱逐页缓存：DMA 需要 CMA 时内核自动腾挪，
    // 非匿名需求降到 ~350MB。代价：CMA 被 stems 挤到极限时 decode 权重
    // 可能回源 eMMC（以 decode t/s 是否塌方为准）。NONE 会双持 912MB
    // 直接 OOM，仍保留仅作对照。
    params.load_mode = LLAMA_LOAD_MODE_DIRECT_IO;
    if (const char * m = getenv("LLMD_LOAD_MODE")) {
        if (!strcmp(m, "AUTO")) params.load_mode = LLAMA_LOAD_MODE_AUTO;
        else if (!strcmp(m, "NONE")) params.load_mode = LLAMA_LOAD_MODE_NONE;
    }
    params.cpuparams.n_threads = 1;
    params.cpuparams_batch.n_threads = 1;
    params.kpu = use_kpu;
    // 启动分期计时：3.5min 的加载黑盒拆开看（2026-09-14 用户反馈开机久）
    auto boot_ms = []() {
        struct timespec ts; clock_gettime(CLOCK_MONOTONIC, &ts);
        return (long long)ts.tv_sec * 1000 + ts.tv_nsec / 1000000;
    };
    long long t_load0 = boot_ms();
    common_init_result_ptr ir = common_init_from_params(params);
    llama_model * model = ir ? ir->model() : nullptr;
    if (!model) { fprintf(stderr, "[llmd] model load failed\n"); return 1; }
    fprintf(stderr, "[llmd] t=%lldms: model+ctx loaded (common_init)\n", boot_ms() - t_load0);
    const llama_vocab * vocab = llama_model_get_vocab(model);
    llama_context * ctx = ir->context();
    if (!ctx) { fprintf(stderr, "[llmd] context init failed\n"); return 1; }
    llama_memory_t mem = llama_get_memory(ctx);

    common_chat_templates_ptr tmpls = common_chat_templates_init(model, "");

    llama_sampler_chain_params sp = llama_sampler_chain_default_params();
    llama_sampler * smpl = llama_sampler_chain_init(sp);
    // 重复惩罚：0.6B 低 temp 极易复读整句（实测），penalty_last_n=64 盖住
    // 生成中的循环半径；RP=1.15 对正常措辞影响有限
    llama_sampler_chain_add(smpl, llama_sampler_init_penalties(
            llama_vocab_n_tokens(vocab), 64, 1.15f, 0.0f, 0.0f));
    if (temp <= 0) {
        llama_sampler_chain_add(smpl, llama_sampler_init_greedy());
    } else {
        if (top_k > 0) llama_sampler_chain_add(smpl, llama_sampler_init_top_k(top_k));
        if (top_p < 1) llama_sampler_chain_add(smpl, llama_sampler_init_top_p(top_p, 1));
        llama_sampler_chain_add(smpl, llama_sampler_init_temp(temp));
        llama_sampler_chain_add(smpl, llama_sampler_init_dist(LLAMA_DEFAULT_SEED));
    }

    // 会话状态
    std::vector<common_chat_msg> history;
    if (!system_prompt.empty()) {
        common_chat_msg sm;
        sm.role = "system";
        sm.content = system_prompt;
        history.push_back(std::move(sm));
    }
    // system 段 token 数：滑窗淘汰首轮时保留 system、只删对话体
    size_t sys_len = 0;
    if (!history.empty()) {
        std::vector<common_chat_msg> sys_only(history.begin(), history.begin() + 1);
        Render rs = render_chat(tmpls.get(), sys_only, "", vocab, false);
        std::string sbase = render_base(rs.str);
        if (!sbase.empty())
            sys_len = common_tokenize(vocab, sbase, false, true).size();
    }
    // delta 头部固定是上轮收尾的 "<|im_end|>\n"：span 右移它到 user 段头，
    // 避免淘汰后保留区开头残留孤儿 im_end
    const size_t n_head = common_tokenize(vocab, "<|im_end|>\n", false, true).size();
    std::vector<llama_token> kv_tokens;  // KV 中当前的完整 token 序列（随滑窗维护）
    std::vector<size_t> turn_start;      // 各轮 user 段在 kv_tokens 的起始下标
    turn_start.push_back(0);             // [0] = system 段起点
    int n_session = 0;

    fprintf(stderr, "[llmd] t=%lldms: sampler/templates done, serving\n", boot_ms() - t_load0);
    printf("READY\n");
    fflush(stdout);

    std::string dline;
    for (;;) {
        if (!std::getline(std::cin, dline)) {
            std::cin.clear();
            // FIFO 写端全部关闭时 read 会返回 EOF：holder 常驻时不该发生，
            // 真发生了就等下一个写者唤醒（不退出，模型不能丢）
            usleep(200 * 1000);
            continue;
        }
        if (dline == "QUIT") break;
        if (dline.empty() || dline[0] == '#') continue;

        size_t q1 = dline.find('\t');
        size_t q2 = (q1 == std::string::npos) ? std::string::npos
                                              : dline.find('\t', q1 + 1);
        if (q1 == std::string::npos || q2 == std::string::npos) {
            printf("ERROR protocol: expected <out>\\t<n_predict>\\t<text>\n");
            fflush(stdout);
            continue;
        }
        std::string out_path = dline.substr(0, q1);
        int n_predict = atoi(dline.substr(q1 + 1, q2 - q1 - 1).c_str());
        if (n_predict <= 0) n_predict = n_predict_def;
        std::string user = dline.substr(q2 + 1);
        if (user.empty() || out_path.empty()) continue;
        // 预热轮识别（chatd 开机 warmup 写 /tmp/chat/warmup_out.txt）：
        // 该轮照常 prefill 垫 KV，但静音 TTS——用户听不到开机自语
        g_tts_mute = out_path.find("/warmup") != std::string::npos;

        // 滑窗淘汰一步：从 KV 中间删除最老一轮的 token 段并压实位置
        // （seq_rm + seq_add），历史同步删除。返回 false = 没得淘汰了。
        auto evict_one = [&]() -> bool {
            if (turn_start.size() <= 1 || history.size() <= 1) return false;
            // mark 指向 delta 起点（上轮收尾 im_end 处）：span 右移 n_head
            // 到 user 段头，孤儿 im_end 归入被淘汰侧。首轮 mark=0 覆盖
            // system 段，从 sys_len 切起保留 system。
            size_t s0 = turn_start[1] == 0 ? sys_len : turn_start[1] + n_head;
            size_t s1 = turn_start.size() > 2
                            ? turn_start[2] + n_head
                            : kv_tokens.size();
            if (s1 <= s0 || s1 > kv_tokens.size()) return false;
            llama_memory_seq_rm(mem, 0, (llama_pos)s0, (llama_pos)s1);
            llama_memory_seq_add(mem, 0, (llama_pos)s0, -1, -((llama_pos)(s1 - s0)));
            kv_tokens.erase(kv_tokens.begin() + s0, kv_tokens.begin() + s1);
            for (size_t i = 2; i < turn_start.size(); ++i) turn_start[i] -= (s1 - s0);
            turn_start.erase(turn_start.begin() + 1);
            history.erase(history.begin() + 1,
                          history.begin() + std::min((size_t)3, history.size()));
            return true;
        };

        // 渲染本轮完整 prompt（字符串 + token）
        Render r = render_chat(tmpls.get(), history, user, vocab);
        // 预估淘汰（全量渲染只是估算；delta 路径按实际需要还会再淘汰）
        while ((int)r.tok.size() + n_predict + 16 > n_ctx
               && evict_one())
            r = render_chat(tmpls.get(), history, user, vocab);
        if ((int)r.tok.size() + n_predict + 16 > n_ctx) {
            // 极端情况：光本轮就超预算，硬截前缀（丢 system 也在所不惜）；
            // 字符串与 token 不再对应，禁用增量路径
            int keep = n_ctx - n_predict - 16;
            r.tok.erase(r.tok.begin(), r.tok.end() - std::min(keep, (int)r.tok.size()));
            r.str.clear();
            kv_tokens.clear();
            turn_start.clear();
            turn_start.push_back(0);
            llama_memory_clear(mem, 0);
        }

        auto t0 = clk::now();
        bool failed = false;
        bool appended = false;
        size_t L = 0;
        size_t n_prefill = 0;

        // 增量路径（常态）。模板渲染不是纯增量：最后一条 assistant 消息会被
        // 注入空 think 块（enable_thinking=false），所以孪生渲染 r0 也要补
        // 一条空 user 消息保证形状一致，历史段才能逐字节可比：
        //   r0.str = FOR + EUS + 生成提示      （FOR = 历史完整渲染）
        //   r.str  = FOR + 新user 段 + 生成提示
        // 增量从上轮回复的收尾 <|im_end|> 切起（EOG 采样到就停了没 decode），
        // KV 已有内容——包括采样出的回复 id——一个 token 都不动。
        // 拼出来装不下就继续淘汰最老一轮再试（增量段本身不动）。
        for (int att = 0; att < 8 && !failed && !appended; ++att) {
            bool ok = false;
            std::string dstr;
            if (n_session > 0 && !kv_tokens.empty() && !r.str.empty()) {
                static const std::string EUS = "<|im_start|>user\n<|im_end|>\n";
                Render r0 = render_chat(tmpls.get(), history, "", vocab);
                std::string rbs = render_base(r0.str);   // = FOR + EUS
                std::string rs = render_base(r.str);     // = FOR + 新 user 段
                ok = rbs.size() > EUS.size()
                     && rbs.compare(rbs.size() - EUS.size(), EUS.size(), EUS) == 0;
                if (ok) {
                    std::string f = rbs.substr(0, rbs.size() - EUS.size());
                    ok = !f.empty() && r.str.size() > f.size()
                         && r.str.compare(0, f.size(), f) == 0
                         && rs.size() > f.size()
                         && rs.compare(0, f.size(), f) == 0;
                    if (ok) {
                        // rs 里倒数第二个 <|im_end|> = 上轮回复的收尾
                        size_t last_im = rs.rfind("<|im_end|>");
                        size_t sec = (last_im == std::string::npos || last_im == 0)
                                         ? std::string::npos
                                         : rs.rfind("<|im_end|>", last_im - 1);
                        size_t gp = r.str.rfind("<|im_start|>assistant");
                        if (sec != std::string::npos && gp != std::string::npos
                                && gp > sec)
                            dstr = rs.substr(sec) + r.str.substr(gp);
                        else
                            ok = false;
                    }
                }
            }
            std::vector<llama_token> dtok;
            if (ok && !dstr.empty()) dtok = common_tokenize(vocab, dstr, false, true);
            if (!dtok.empty()
                    && (int)(kv_tokens.size() + dtok.size()) + n_predict + 16
                           <= n_ctx) {
                fprintf(stderr, "[llmd] turn %d: append delta %zu tok (kv %zu)\n",
                        n_session, dtok.size(), kv_tokens.size());
                for (size_t off = 0; off < dtok.size() && !failed; ) {
                    size_t n = std::min((size_t)1024, dtok.size() - off);
                    llama_batch b = llama_batch_get_one(dtok.data() + off, (int)n);
                    if (llama_decode(ctx, b)) { failed = true; break; }
                    off += n;
                }
                if (!failed) {
                    n_prefill = dtok.size();
                    turn_start.push_back(kv_tokens.size());
                    kv_tokens.insert(kv_tokens.end(), dtok.begin(), dtok.end());
                    appended = true;
                }
            } else if (dtok.empty()) {
                break;   // 校验没过：模板形状意外，走兜底续算
            } else if (evict_one()) {
                r = render_chat(tmpls.get(), history, user, vocab);
            } else {
                break;   // 没得淘汰了：走兜底
            }
        }

        // 全量/前缀路径：首 turn、增量校验失败、或增量后仍超限时兜底。
        // 前缀断裂也不再清空整窗：从断点续算，损失只限最后一轮的长度。
        // 心跳：看门狗（chatd 3min 无增长即杀）与现场定位靠它区分卡在
        // prefill 还是 decode（2026-09-14 实录：静默 10min 只能盲杀）
        fprintf(stderr, "[llmd] hb: turn %d start, prompt %zu tok\n",
                n_session, r.tok.size());
        if (!appended && !failed) {
            size_t mx = std::min(kv_tokens.size(), r.tok.size());
            while (L < mx && kv_tokens[L] == r.tok[L]) L++;
            if (L < kv_tokens.size()) {
                fprintf(stderr, "[llmd] turn %d: prefix break %zu < %zu, "
                                "prefill from break\n", n_session, L, kv_tokens.size());
                for (size_t i = 0; i < turn_start.size(); ++i) {
                    if (turn_start[i] > L) { turn_start.resize(i); break; }
                }
                kv_tokens.resize(L);
                llama_memory_seq_rm(mem, 0, (llama_pos)L, -1);
            }
            if (L > 0) L = std::min(L, r.tok.size() - 1);  // 至少留 1 token 驱动采样
            if (L == 0) llama_memory_clear(mem, 0);
            for (size_t off = L; off < r.tok.size() && !failed; ) {
                size_t n = std::min((size_t)1024, r.tok.size() - off);
                llama_batch b = llama_batch_get_one(r.tok.data() + off, (int)n);
                if (llama_decode(ctx, b)) { failed = true; break; }
                off += n;
            }
            if (!failed) {
                n_prefill = r.tok.size() - L;
                turn_start.push_back(L);
                kv_tokens = r.tok;
            }
        }

        clk::time_point t_pref_end = clk::now(), t_first = t_pref_end;
        double pf_sec = std::chrono::duration<double>(t_pref_end - t0).count();

        fprintf(stderr, "[llmd] turn %d: prompt %zu tok, kv %zu reuse %zu, gen cap %d\n",
                n_session, r.tok.size(), kv_tokens.size(), L, n_predict);

        TurnOut to;
        if (failed || !to.open(out_path.c_str())) {
            printf("DONE %s 0 0 0\n", out_path.c_str());
            fflush(stdout);
            kv_tokens.clear();
            turn_start.clear();
            turn_start.push_back(0);
            llama_memory_clear(mem, 0);
            continue;
        }

        int n_gen = 0;
        std::string reply;
        std::string pending;   // 未凑满一句的尾巴（流水 TTS）
        while (n_gen < n_predict) {
            llama_token id = llama_sampler_sample(smpl, ctx, -1);
            // Qwen3 enable_thinking=false 时模型偶发在注入的空 think 块后
            // 直接 EOS（0 内容）；前几个 token 无视 EOG 强制继续
            if (llama_vocab_is_eog(vocab, id) && n_gen >= 4) break;
            if (llama_vocab_is_eog(vocab, id)) continue;
            char buf[192];
            int n = llama_token_to_piece(vocab, id, buf, sizeof(buf), 0, true);
            if (n < 0) break;
            to.write(buf, n);
            reply.append(buf, n);
            // 流水 TTS：按标点白名单切段（见注释），文本同步 fflush 流水打印
            pending.append(buf, n);
            size_t cut = idx_after_first_sep(pending);
            while (cut != std::string::npos) {
                if (cut > 0) {
                    tts_send_sentence(pending.substr(0, cut));
                    fflush(to.f);
                }
                pending.erase(0, cut);
                cut = idx_after_first_sep(pending);
            }
            kv_tokens.push_back(id);
            n_gen++;
            if (n_gen % 16 == 0)
                fprintf(stderr, "[llmd] hb: turn %d decode %d tok\n",
                        n_session, n_gen);
            if (n_gen == 1) {
                // 首包必须尽快可见（demo 在轮询文件大小）
                fflush(to.f);
                t_first = clk::now();
            }
            llama_batch b = llama_batch_get_one(&id, 1);
            if (llama_decode(ctx, b)) { failed = true; break; }
        }
        if (!pending.empty()) tts_send_sentence(pending);
        pending.clear();
        fflush(to.f);
        to.close();

        if (failed) {
            // decode 半途出错：KV 实际内容已不可信，记账作废，
            // 下一轮全量重算（daemon 不退出，服务不中断）
            kv_tokens.clear();
            turn_start.clear();
            turn_start.push_back(0);
            llama_memory_clear(mem, 0);
        }

        // 记入历史（供下一轮渲染）
        common_chat_msg um, am;
        um.role = "user";  um.content = user;
        am.role = "assistant"; am.content = reply;
        history.push_back(std::move(um));
        history.push_back(std::move(am));
        n_session++;

        double sec = std::chrono::duration<double>(clk::now() - t0).count();
        double ft_sec = std::chrono::duration<double>(t_first - t0).count();
        double dec_sec = std::chrono::duration<double>(clk::now() - t_first).count();
        fprintf(stderr,
                "[llmd] turn %d PERF: prefill %.2fs (%zu tok, %.2f t/s) | "
                "first-tok %.2fs | decode %.2fs (%d tok, %.2f t/s) | total %.2fs\n",
                n_session - 1, pf_sec, n_prefill,
                pf_sec > 0 ? n_prefill / pf_sec : 0.0,
                ft_sec, dec_sec, n_gen > 0 ? n_gen - 1 : 0,
                dec_sec > 0 ? (n_gen > 1 ? (n_gen - 1) / dec_sec : 0.0) : 0.0,
                sec);
        printf("DONE %s %d %.2f %.2f\n", out_path.c_str(), n_gen, sec,
               sec > 0 ? n_gen / sec : 0.0);
        fflush(stdout);
    }

    llama_sampler_free(smpl);
    llama_free(ctx);
    llama_model_free(model);
    printf("BYE\n");
    return 0;
}
