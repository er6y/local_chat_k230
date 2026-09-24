// kpud.cpp — KPU GEMM 守护（C++ 版，替代 kpu_gemm_daemon.py）
//
// 线协议与 Python 版字节级兼容（llmd 的 kpu_gemm.cpp 客户端零改动）：
//   请求: <u32 magic=0x4B505547><u16 stem_len><stem><u32 mt><f32 x[mt*K]>
//   成功: <u32 magic><u32 code=0><f32 y[mt*OUT]>
//   失败: <u32 magic><u32 code=1>            （定长 8B！错误详情只进日志。
//          历史教训：变长错误串会留在流里被下次请求当成响应头→协议永久失步）
//
// 行为契约（与 Python 版一致，勿改语义）：
//   - S 选择: mt==1→S1, mt<=16→S4, 否则 S16；目录 KPUD_S{1,4,16}_DIR
//   - CAPS[达上限直接拒绝]，绝不驱逐（解释器析构实测段错误）
//   - IO_SCALE: 输入乘 s 输出除 s（量化裕量，GEMM 线性），仅 sc 缺失时
//   - warm: 先全量加载到 cap、逐个试跑钉 CMA，再进 serve 循环
//
// 待板调项（代码完成后联调）：
//   - O_DIRECT 读 kmodel 的对齐缓冲路径
//   - warm 期 CmaFree 与 Python 版逐项对账
#include <nncase/api.h>
#include <nncase/runtime/simple_types.h>
#include <nncase/runtime/interpreter.h>
#include <nncase/runtime/runtime_tensor.h>

#include <sys/socket.h>
#include <sys/un.h>
#include <sys/prctl.h>
#include <poll.h>
#include <fcntl.h>
#include <unistd.h>
#include <signal.h>
#include <dlfcn.h>
#include <errno.h>
#include <sys/mman.h>
// vendor GNNE 硬件初始化（k230 runtime 模块内实现）；gnne_regs 全局见 main
extern "C" void gnne_init(void);
// k230 runtime 的寄存器窗口全局（gnne.c.o，whole-archive 带入）。
// 本镜像 /dev/k230-gnne 无 mmap 回调 → runtime 自映射失败留 NULL → 所有
// 子图静默空转。这里直接 extern 引用（静态链接，dlsym 对可执行文件无效）。
extern "C" void *gnne_regs;
#include <dirent.h>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <string>
#include <vector>
#include <map>
#include <algorithm>
#include <chrono>

static const uint32_t MAGIC = 0x4B505547;
static const char *SOCK_PATH = "/tmp/kpu_gemm.sock";

// mmz_shim.c（同源 C）：kmodel 必须放 CMA —— 该 API 把 GNNE 的取指 pc_s/pc_e
// 指进这块缓冲，malloc 缓冲(普通 DDR)会锁死内存总线硬挂板（历史复现两次）。
extern "C" {
int kd_mpi_sys_mmz_alloc_cached(uint64_t *phy_addr, void **virt_addr,
                                const char *mmb, const char *zone, uint32_t len);
int kd_mpi_sys_mmz_free(uint64_t phy_addr, void *virt_addr);
int kd_mpi_sys_mmz_flush_cache(uint64_t phy, void *virt, uint32_t len);
}

// ---- 配置（与 Python 版同名 env）----
static std::string DIR_S16, DIR_S4, DIR_S1;
static int CAP1, CAP4, CAP16;
static float IO_SCALE = 1.0f;
static bool WARM = false;

static void logf_(const char *fmt, ...) {
    va_list ap;
    struct timespec ts;
    clock_gettime(CLOCK_REALTIME, &ts);
    fprintf(stderr, "[kpud %ld.%03ld] ", (long)ts.tv_sec, ts.tv_nsec / 1000000);
    va_start(ap, fmt);
    vfprintf(stderr, fmt, ap);
    va_end(ap);
    fputc('\n', stderr);
    fflush(stderr);
}

// ---- stem → (K, OUT)；命名契约 l{n}_{q|k|v|o|gate|up|down} ----
struct Spec { int K, OUT; };
static bool infer_shape(const std::string &stem, int &K, int &OUT) {
    size_t us = stem.find('_');
    if (us == std::string::npos) return false;
    std::string sfx = stem.substr(us + 1);
    if (sfx == "q")          { K = 1024; OUT = 2048; return true; }
    if (sfx == "k")          { K = 1024; OUT = 1024; return true; }
    if (sfx == "v")          { K = 1024; OUT = 1024; return true; }
    if (sfx == "o")          { K = 2048; OUT = 1024; return true; }
    if (sfx == "gate")       { K = 1024; OUT = 3072; return true; }
    if (sfx == "up")         { K = 1024; OUT = 3072; return true; }
    if (sfx == "down")       { K = 3072; OUT = 1024; return true; }
    return false;
}

// ---- 解释器缓存：满即拒，绝不驱逐 ----
struct Entry {
    nncase::runtime::interpreter *interp = nullptr;
    int K = 0, OUT = 0, S = 16;
    // 输入 tile 缓冲常驻复用（加载时 zero 一次），**但必须维持不变量：
    // 列 [mt, MT) 恒为 0**。kmodel 的输入量化是整块 per-tensor 的，尾巴里
    // 的非零陈旧数据会抬高整块动态范围，把真实行压到 0 —— 实测 M=57 连续调用
    // （尾巴一直是 warm 留下的 0）输出正确，一旦转到 M=18，列 [18,57) 还留着
    // 上一张图 l0_q 的真实激活，输出立刻变乱码。
    // 早先"空间维不混合所以 padding 无所谓"的判断只对输出行有效，对输入
    // 量化范围无效。last_mt 记录上次写入的列数，只在变小的时候补零。
    std::vector<float> inbuf;
    int last_mt = 0;   // 已写入（或已确认清零）的列数
    // kmodel 缓冲（mmz/CMA 背板）。load_model(copy=false) 原地引用它：
    // copy=true 会让 runtime 再拷一份，实测每根 stem 多花一整个文件大小
    // （3.2MB 文件的 "model" 项实测 6.4MB）——CMA 是生命线，这份拷贝必须省。
    uint8_t *mbuf = nullptr;
    uint64_t mbuf_phy = 0;
};
static std::map<std::pair<std::string, int>, Entry> g_cache;
static std::vector<std::pair<std::string, int>> g_order;   // LRU 记账（仅计数用）

static int cap_for(int S) { return S == 1 ? CAP1 : (S == 4 ? CAP4 : CAP16); }
static const std::string &dir_for(int S) {
    static const std::string d1 = DIR_S1, d4 = DIR_S4, d16 = DIR_S16;
    return S == 1 ? d1 : (S == 4 ? d4 : d16);
}

// O_DIRECT 读（页缓存会挤占 CMA，与 Python 版 _read_kmodel_nocache 同理）
static bool read_kmodel_nocache(const std::string &path, std::vector<uint8_t> &out) {
    // O_DIRECT 要求 buffer/长度扇区对齐：posix_memalign 对齐缓冲读再搬入
    // vector（Python 版对齐 mmap 同理）；无 O_DIRECT 支持则普通读
    int fd = open(path.c_str(), O_RDONLY | O_DIRECT);
    bool odirect = fd >= 0;
    if (!odirect) fd = open(path.c_str(), O_RDONLY);
    if (fd < 0) return false;
    off_t len = lseek(fd, 0, SEEK_END);
    lseek(fd, 0, SEEK_SET);
    if (len <= 0) { close(fd); return false; }
    size_t alen = odirect ? (((size_t)len + 4095) & ~4095UL) : (size_t)len;
    uint8_t *abuf = nullptr;
    if (odirect && posix_memalign((void **)&abuf, 4096, alen) != 0) {
        close(fd);
        return false;
    }
    uint8_t *dst = abuf;
    if (!abuf) { out.resize((size_t)len); dst = out.data(); }
    size_t got = 0;
    while (got < alen) {
        ssize_t n = read(fd, dst + got, alen - got);
        if (n <= 0) break;
        got += (size_t)n;
    }
    close(fd);
    bool ok = got >= (size_t)len;
    if (ok && abuf) out.assign(abuf, abuf + len);
    free(abuf);
    return ok;
}

static int count_for(int S) {
    int n = 0;
    for (auto &k : g_order)
        if (k.second == S) n++;
    return n;
}

// CMA 地板：低于此值不再加载新解释器。vendor runtime 的 order:10 分配失败
// 不查错直接解引用 → SIGSEGV → 整机挂死（历史多次）。宁可少驻留几根走 CPU。
static const long CMA_FLOOR_KB = 96 * 1024;

static long cma_free_kb();   // 定义见 warm 段之前

static bool load_entry(const std::string &stem, int S, Entry &e) {
    // 每档严格 cap（Python 版同语义）：CMA 是生命线，超 cap 一律拒绝
    // 走客户端 CPU 回退。懒加载不算免费——l8 以后的请求会逐根吃 CMA，
    // 总池化的"简化"曾把 CMA 掏到 18MB → GNNE order-10 失败 → 挂死
    if (cap_for(S) <= 0 || count_for(S) >= cap_for(S)) {
        return false;
    }
    long cmaf = cma_free_kb();
    if (cmaf >= 0 && cmaf < CMA_FLOOR_KB) {
        logf_("LOAD REFUSED %s: CmaFree %ld kB < floor %ld kB", stem.c_str(), cmaf,
              CMA_FLOOR_KB);
        return false;
    }
    char path[512];
    snprintf(path, sizeof path, "%s/%s.kmodel", dir_for(S).c_str(), stem.c_str());
    long cma_before = cma_free_kb();
    std::vector<uint8_t> blob;
    if (!read_kmodel_nocache(path, blob)) {
        logf_("LOAD FAIL %s (read)", path);
        return false;
    }
    const size_t mlen = blob.size();
    // 读进 mmz/CMA 缓冲：load_model(copy=false) 原地引用 → 权重只存一份
    // （copy=true 时 runtime 再拷一份，每 stem 白吃一个文件大小：实测 3.2MB
    //  的文件 model 项吃 6.4MB）。CMA 是生命线，这份拷贝必须省。
    uint64_t mb_phy = 0;
    uint8_t *mb = nullptr;
    if (kd_mpi_sys_mmz_alloc_cached(&mb_phy, (void **)&mb, NULL, NULL,
                                    (uint32_t)mlen) == 0 && mb) {
        memcpy(mb, blob.data(), mlen);
        kd_mpi_sys_mmz_flush_cache(0, mb, (uint32_t)mlen);
        std::vector<uint8_t>().swap(blob);   // 立刻还掉临时堆缓冲
    } else {
        logf_("LOAD WARN %s: mmz alloc failed (%zu B), fall back to model copy",
              path, mlen);
        mb_phy = 0;
        mb = nullptr;
    }
    const uint8_t *msrc = mb ? mb : blob.data();
    auto *it = new nncase::runtime::interpreter();
    auto lr = it->load_model(
        gsl::span<const gsl::byte>((const gsl::byte *)msrc, (gsl::index)mlen),
        mb == nullptr);   // mmz 缓冲原地引用，堆缓冲仍需 runtime 自拷
    if (!lr.is_ok()) {
        logf_("LOAD FAIL %s (nncase)", path);
        delete it;
        if (mb_phy) kd_mpi_sys_mmz_free(mb_phy, mb);
        return false;
    }
    // CMA 归因：load_model 之后 vs read 之后 = 权重/模型副本；warm 试跑之后
    // 再涨的 = 运行时激活缓冲（决定 cap 能推多高的关键数）
    long cma_after_model = cma_free_kb();
    auto ish = it->input_shape(0);
    auto osh = it->output_shape(0);
    if (ish.size() < 4 || osh.size() < 4) {
        logf_("LOAD FAIL %s (shape rank)", path);
        delete it;
        return false;
    }
    e.interp = it;
    e.K = (int)ish[1];
    e.OUT = (int)osh[1];
    e.S = (int)ish[2];
    // ★ load 时注册 pool_shared（CMA）输入张量 —— client 的 load_kmodel_locked
    // 同款（M1 验证路径的组成部分）。runtime 首次 input_tensor() 登记 DMA
    // 用的缓冲；若首次注册的是 pool_cpu_only 的普通内存（per-invoke），
    // GNNE 描述符指向不可 DMA 的页 → 子图静默空转（锯齿乱码根因链最后一环）。
    // 之后每次 invoke 仍用 copy=true 的 per-invoke 张量替换，与 client 一致。
    {
        auto itr = nncase::runtime::host_runtime_tensor::create(
            nncase::dt_float32, {1, (size_t)e.K, (size_t)e.S, (size_t)e.S},
            nncase::runtime::host_runtime_tensor::pool_shared);
        if (!itr.is_ok() || !it->input_tensor(0, itr.unwrap()).is_ok()) {
            logf_("LOAD FAIL %s (pool_shared input tensor)", path);
            delete it;
            if (mb_phy) kd_mpi_sys_mmz_free(mb_phy, mb);
            return false;
        }
    }
    e.inbuf.assign((size_t)e.K * e.S * e.S, 0.0f);   // 常驻输入 tile，只零一次
    e.last_mt = e.S * e.S;                           // 整块已确认全 0
    e.mbuf = mb;
    e.mbuf_phy = mb_phy;
    g_cache[{stem, S}] = e;
    g_order.push_back({stem, S});
    logf_("loaded %s in=[1,%d,%d,%d] out=[1,%d,%d,%d] file=%zuKB cma %ld->%ld->%ld KB "
          "(model %ld, io %ld)",
          path, e.K, e.S, e.S, e.OUT, e.S, e.S, mlen / 1024,
          cma_before, cma_after_model, cma_free_kb(),
          cma_before - cma_after_model, cma_after_model - cma_free_kb());
    return true;
}

static Entry *get_entry(const std::string &stem, int S) {
    auto key = std::make_pair(stem, S);
    auto it = g_cache.find(key);
    if (it != g_cache.end()) return &it->second;
    Entry e;
    if (!load_entry(stem, S, e)) return nullptr;
    return &g_cache[key];
}

// ---- invoke 分相累计（每 50 次调用随 stats 一起打）----
struct Phases { long long fold = 0, mk = 0, run = 0, out = 0; } g_ph;

// 一次 invoke：fold → tensor → run → unfold。IO_SCALE 只在无 scale 时代生效
// （plain int8 部署无 sidecar scale；SQ 时代语义已弃）。
// 分相计时（g_ph）用于定位每调用 16.5ms 的构成。
//
// fold/copy-out 都是 16×16 分块转置：原实现 m 外层 / c 内层，写入步长
// MT*4=1KB，每个 4B 存都命中不同 cache line（实测 fold 4.5ms / out 2.3ms，
// 占每调用 43%）。分块后两侧都走满 cache line。
#define TBLK 16
static bool invoke(Entry &e, const float *x, int mt, float *y) {
    using clk = std::chrono::steady_clock;
    const int K = e.K, OUT = e.OUT, S = e.S, MT = S * S;
    auto t0 = clk::now();
    float *inp = e.inbuf.data();
    // 维持"列 [mt, MT) 恒为 0"不变量：M 变小时补零被腾空的列。同 M 连续调用
    // （prefill 一层的 7 个 GEMM、整轮同尺寸）零开销。
    if (mt < e.last_mt) {
        const size_t z = (size_t)(e.last_mt - mt) * sizeof(float);
        for (int c = 0; c < K; c++) memset(inp + (size_t)c * MT + mt, 0, z);
        e.last_mt = mt;
    } else if (mt > e.last_mt) {
        e.last_mt = mt;
    }
    {
        float tile[TBLK][TBLK];
        for (int m0 = 0; m0 < mt; m0 += TBLK) {
            int mi = mt - m0 < TBLK ? mt - m0 : TBLK;
            for (int c0 = 0; c0 < K; c0 += TBLK) {
                int ci = K - c0 < TBLK ? K - c0 : TBLK;
                for (int i = 0; i < mi; i++) {
                    const float *xr = x + (size_t)(m0 + i) * K + c0;
                    for (int j = 0; j < ci; j++) tile[i][j] = xr[j] * IO_SCALE;
                }
                for (int j = 0; j < ci; j++) {
                    float *dst = inp + (size_t)(c0 + j) * MT + m0;
                    for (int i = 0; i < mi; i++) dst[i] = tile[i][j];
                }
            }
        }
    }
    auto t1 = clk::now();
    auto itr = nncase::runtime::host_runtime_tensor::create(
        nncase::dt_float32, {1, (size_t)K, (size_t)S, (size_t)S},
        gsl::as_writable_bytes(gsl::make_span(inp, (size_t)K * MT)),
        true,   // copy=true：M1 验证过的保真路径
        nncase::runtime::host_runtime_tensor::pool_cpu_only);
    if (!itr.is_ok()) return false;
    if (!e.interp->input_tensor(0, itr.unwrap()).is_ok()) return false;
    auto t2 = clk::now();
    if (!e.interp->run().is_ok()) return false;
    auto t3 = clk::now();
    auto otr = e.interp->output_tensor(0);
    if (!otr.is_ok()) return false;
    auto th = otr.unwrap().to_host();
    if (!th.is_ok()) return false;
    auto omr = nncase::runtime::host_runtime_tensor::map(th.unwrap(),
                                                         nncase::runtime::map_read);
    if (!omr.is_ok()) return false;
    const float *src = (const float *)omr.unwrap().buffer().data();
    const float inv = 1.0f / IO_SCALE;
    {
        float tile[TBLK][TBLK];
        for (int m0 = 0; m0 < mt; m0 += TBLK) {
            int mi = mt - m0 < TBLK ? mt - m0 : TBLK;
            for (int o0 = 0; o0 < OUT; o0 += TBLK) {
                int oi = OUT - o0 < TBLK ? OUT - o0 : TBLK;
                for (int j = 0; j < oi; j++) {
                    const float *srow = src + (size_t)(o0 + j) * MT + m0;
                    for (int i = 0; i < mi; i++) tile[i][j] = srow[i] * inv;
                }
                for (int i = 0; i < mi; i++) {
                    float *yr = y + (size_t)(m0 + i) * OUT + o0;
                    for (int j = 0; j < oi; j++) yr[j] = tile[i][j];
                }
            }
        }
    }
    auto t4 = clk::now();
    auto us = [](clk::time_point a, clk::time_point b) {
        return std::chrono::duration_cast<std::chrono::microseconds>(b - a).count();
    };
    g_ph.fold += us(t0, t1);
    g_ph.mk += us(t1, t2);
    g_ph.run += us(t2, t3);
    g_ph.out += us(t3, t4);
    return true;
}

// ---- serve：单线程顺序处理（单核板上多线程无收益）----
static volatile sig_atomic_t g_stop = 0;
static void on_term(int) { g_stop = 1; }

static int recvn(int fd, void *buf, size_t n) {
    size_t got = 0;
    while (got < n) {
        ssize_t r = recv(fd, (char *)buf + got, n - got, 0);
        if (r <= 0) return -1;
        got += (size_t)r;
    }
    return 0;
}

struct Stats { long calls = 0, errs = 0; long long us = 0; } g_st;

// 排空请求体。客户端把 <mt,f32 x[mt*K]> 与头部打在同一次 sendmsg 里，
// 拒绝时若不读掉，残留字节会被下一请求的 magic 校验吃掉 → 断连 → 客户端
// 重连风暴（实测：cap50 一轮 prefill 144 次拒绝 → 72 次失步）。K 由 stem
// 后缀推出；推不出（名字非法）就只能断连——按协议这是唯一安全选择。
static bool drain_payload(int conn, const char *stem, uint32_t mt) {
    int K, OUT;
    if (mt == 0 || mt > 65536 || !infer_shape(stem, K, OUT)) return false;
    size_t left = (size_t)mt * (size_t)K * 4;
    char buf[16384];
    while (left) {
        size_t n = left < sizeof buf ? left : sizeof buf;
        ssize_t r = recv(conn, buf, n, 0);
        if (r <= 0) return false;
        left -= (size_t)r;
    }
    return true;
}

static bool send_err(int conn, uint32_t code) {
    uint32_t hdr[2] = {MAGIC, code};
    return send(conn, hdr, 8, MSG_NOSIGNAL) == 8;
}

static void handle_conn(int conn) {
    // 长连接多请求：llmd 一个连接跑成千上万个 tile（Python 版同语义），
    // 一请求一关是致命 bug（第二个请求即 EPIPE）
    while (true) {
    uint32_t magic;
    uint16_t slen;
    if (recvn(conn, &magic, 4) || recvn(conn, &slen, 2)) return;
    if (magic != MAGIC) return;
    char stem[64] = {0};
    if (slen >= sizeof stem || recvn(conn, stem, slen)) return;
    uint32_t mt;
    if (recvn(conn, &mt, 4)) return;
    if (mt == 0 || mt > 65536) { goto err_drain; }
    {
        int S = (mt == 1) ? 1 : (mt <= 16 ? 4 : 16);
        Entry *e = get_entry(stem, S);
        if (!e) {
            logf_("ERR %s: no kmodel S%d (cap=%d)", stem, S, cap_for(S));
            goto err_drain;
        }
        int K = e->K, OUT = e->OUT;
        std::vector<float> x((size_t)mt * K);
        if (recvn(conn, x.data(), x.size() * 4)) return;
        auto t0 = std::chrono::steady_clock::now();
        std::vector<float> y((size_t)mt * OUT);
        bool ok = invoke(*e, x.data(), (int)mt, y.data());
        long long dt = std::chrono::duration_cast<std::chrono::microseconds>(
                           std::chrono::steady_clock::now() - t0).count();
        g_st.calls++;
        g_st.us += dt;
        // KPUD_TRACE=1：逐调用打点（定位小 M 调用异常变慢——M=57 实测 8.6ms
        // 但增量轮按模型反推要 40ms，必须看到单次真数）
        static const bool trace = getenv("KPUD_TRACE") != nullptr;
        if (trace)
            logf_("CALL %s mt=%u %lldus", stem, mt, dt);
        if (!ok) {
            g_st.errs++;
            logf_("ERR %s: invoke failed", stem);
            goto err_nodrain;   // 请求体已读，不能再排空（会吃掉下一个请求）
        }
        uint32_t hdr[2] = {MAGIC, 0};
        if (send(conn, hdr, 8, MSG_NOSIGNAL) != 8 ||
            send(conn, y.data(), y.size() * 4, MSG_NOSIGNAL) != (ssize_t)(y.size() * 4)) {
            logf_("send fail (client gone)");
            return;
        }
        if (g_st.calls % 50 == 0)
            logf_("calls=%ld avg=%lldus [fold=%lld mk=%lld run=%lld out=%lld] errs=%ld",
                  g_st.calls, g_st.us / g_st.calls,
                  g_ph.fold / g_st.calls, g_ph.mk / g_st.calls,
                  g_ph.run / g_st.calls, g_ph.out / g_st.calls, g_st.errs);
        continue;   // 长连接：本请求完成，读下一个
    }
err_drain:
    // code=2：daemon 侧永久拒绝（cap 满 / 无 kmodel）。客户端据此把该 stem
    // 从 manifest 摘掉，此后直接走 CPU，不再付往返成本。
    if (!send_err(conn, 2)) return;
    if (!drain_payload(conn, stem, mt)) return;   // 排不干净就断连，绝不带脏流
    continue;
err_nodrain:
    if (!send_err(conn, 1)) return;
    }
}

static long cma_free_kb() {
    FILE *mf = fopen("/proc/meminfo", "r");
    if (!mf) return -1;
    char line[256];
    long v = -1;
    while (fgets(line, sizeof line, mf))
        if (!strncmp(line, "CmaFree:", 8)) { v = atol(line + 8); break; }
    fclose(mf);
    return v;
}

static void warm_all() {
    const std::string &dir = DIR_S16;   // svc 只开 S16 档；其他档按需扩展
    DIR *d = opendir(dir.c_str());
    if (!d) { logf_("warm: no dir %s", dir.c_str()); return; }
    std::vector<std::string> files;
    struct dirent *de;
    while ((de = readdir(d))) {
        std::string n = de->d_name;
        if (n.size() > 7 && n.compare(n.size() - 7, 7, ".kmodel") == 0)
            files.push_back(n.substr(0, n.size() - 7));
    }
    closedir(d);
    std::sort(files.begin(), files.end());
    int ok = 0, n = std::min<int>((int)files.size(), CAP16);
    long cma0 = cma_free_kb();
    logf_("warm start: %d stems, CmaFree %ld kB", n, cma0);
    auto t0 = std::chrono::steady_clock::now();
    for (int i = 0; i < n; i++) {
        // CMA 地板：撞了就停手，剩下的 stem 走 CPU 回退（比 vendor 段错误好）
        long cf = cma_free_kb();
        if (cf >= 0 && cf < CMA_FLOOR_KB) {
            logf_("warm STOP at %d/%d: CmaFree %ld kB < floor %ld kB", i, n, cf,
                  CMA_FLOOR_KB);
            break;
        }
        Entry *e = get_entry(files[i], 16);
        if (!e) continue;
        std::vector<float> dummy((size_t)e->K * e->S * e->S, 0.0f);
        std::vector<float> y((size_t)e->OUT * e->S * e->S);
        if (invoke(*e, dummy.data(), e->S * e->S, y.data())) ok++;
        else logf_("WARMRUN FAIL %s", files[i].c_str());
        // 前 20 根逐根打点：load_model 后 vs 试跑后，分离"模型"与"激活缓冲"
        if (i < 20)
            logf_("warm[%d] %s after-run CmaFree %ld kB", i, files[i].c_str(),
                  cma_free_kb());
        // 逐段打点：CAP 决策靠这张表（每根 stem 的边际 CMA 成本不同）
        if ((i + 1) % 10 == 0 || i + 1 == n) {
            long f = cma_free_kb();
            logf_("warm %d/%d ok=%d CmaFree %ld kB (delta %ld KB total, %.0f KB/stem)",
                  i + 1, n, ok, f, cma0 - f,
                  (double)(cma0 - f) / (double)(i + 1));
        }
    }
    logf_("warmed %d/%d S16 interpreters in %.1fs. CmaFree: %ld kB (%.0f KB/stem overall)",
          ok, n,
          std::chrono::duration<double>(std::chrono::steady_clock::now() - t0).count(),
          cma_free_kb(), (double)(cma0 - cma_free_kb()) / (double)std::max(1, n));
}

int main() {
    // chatd 子进程身份：父死即 TERM（PR_SET_PDEATHSIG 跨普通 exec 保留）
    prctl(PR_SET_PDEATHSIG, SIGTERM, 0, 0, 0);
    if (getppid() == 1) return 1;   // fork 与 prctl 之间父进程已死的竞态补刀

    // ★ gnne_regs 预种 + gnne_init（2026-09-13 定位的本守护空转根因）：
    // 本镜像 /dev/k230-gnne 驱动没有 mmap 回调，vendor runtime 自己映射寄存器
    // 会失败、gnne_regs 留 NULL —— 之后每个 k230 子图静默空转（invoke 返回
    // 成功但 GNNE 从未计算，输出=输入侧数据流的残留，锯齿状乱码）。
    // client 的 local 路径一直有这两步（M1 时代 cos=1.0 的前提）；daemon 侧
    // 一直缺失。必须在任何 load/invoke 之前做。
    {
        if (!gnne_regs) {
            int mfd = open("/dev/mem", O_RDWR | O_SYNC);
            if (mfd >= 0) {
                void *regs = mmap(NULL, 0x1000, PROT_READ | PROT_WRITE,
                                  MAP_SHARED, mfd, 0x80400000ULL);
                close(mfd);
                if (regs != MAP_FAILED) {
                    gnne_regs = regs;
                    logf_("gnne_regs preseeded via /dev/mem: %p", regs);
                } else {
                    logf_("FATAL: mmap /dev/mem 0x80400000 failed: %s",
                          strerror(errno));
                }
            } else {
                logf_("FATAL: open /dev/mem failed: %s", strerror(errno));
            }
        } else {
            logf_("gnne_regs already set: %p", gnne_regs);
        }
        logf_("calling gnne_init");
        gnne_init();
        logf_("gnne_init done");
    }

    auto gs = [](const char *k, const char *d) {
        const char *v = getenv(k);
        return v ? std::string(v) : std::string(d);
    };
    DIR_S16 = gs("KPUD_S16_DIR", "/mnt/data/kpu_qwen/s16b");
    DIR_S4 = gs("KPUD_S4_DIR", "/mnt/data/kpu_qwen/s4");
    DIR_S1 = gs("KPUD_S1_DIR", "/mnt/data/kpu_qwen/s1");
    CAP1 = atoi(gs("KPUD_CAP1", "0").c_str());
    CAP4 = atoi(gs("KPUD_CAP4", "0").c_str());
    CAP16 = atoi(gs("KPUD_CAP16", "0").c_str());
    IO_SCALE = atof(gs("KPUD_IO_SCALE", "1.0").c_str());
    WARM = gs("KPUD_WARM", "0") == "1";
    if (IO_SCALE <= 0.0f) IO_SCALE = 1.0f;

    signal(SIGTERM, on_term);
    signal(SIGINT, on_term);
    signal(SIGPIPE, SIG_IGN);

    unlink(SOCK_PATH);
    int sk = socket(AF_UNIX, SOCK_STREAM, 0);
    sockaddr_un addr{};
    addr.sun_family = AF_UNIX;
    strncpy(addr.sun_path, SOCK_PATH, sizeof addr.sun_path - 1);
    if (bind(sk, (sockaddr *)&addr, sizeof addr) < 0) { perror("bind"); return 1; }
    listen(sk, 4);
    logf_("listening on %s; tier caps {1:%d, 4:%d, 16:%d} io_scale=%.2f warm=%d",
          SOCK_PATH, CAP1, CAP4, CAP16, IO_SCALE, (int)WARM);

    if (WARM) warm_all();

    while (!g_stop) {
        struct pollfd pfd = {sk, POLLIN, 0};
        int pr = poll(&pfd, 1, 5000);          // 5s 醒一次：查父进程是否已死
        if (pr <= 0) {
            if (getppid() == 1) { logf_("parent gone, exit"); break; }
            continue;
        }
        int conn = accept(sk, nullptr, nullptr);
        if (conn < 0) continue;
        handle_conn(conn);
        close(conn);
    }
    logf_("SIGTERM: exit (kernel reclaims on process death)");
    return 0;
}
