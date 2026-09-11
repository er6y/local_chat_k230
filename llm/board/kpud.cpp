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

static bool load_entry(const std::string &stem, int S, Entry &e) {
    // 每档严格 cap（Python 版同语义）：CMA 是生命线，超 cap 一律拒绝
    // 走客户端 CPU 回退。懒加载不算免费——l8 以后的请求会逐根吃 CMA，
    // 总池化的"简化"曾把 CMA 掏到 18MB → GNNE order-10 失败 → 挂死
    if (cap_for(S) <= 0 || count_for(S) >= cap_for(S)) {
        return false;
    }
    char path[512];
    snprintf(path, sizeof path, "%s/%s.kmodel", dir_for(S).c_str(), stem.c_str());
    std::vector<uint8_t> blob;
    if (!read_kmodel_nocache(path, blob)) {
        logf_("LOAD FAIL %s (read)", path);
        return false;
    }
    auto *it = new nncase::runtime::interpreter();
    auto lr = it->load_model(
        gsl::span<const gsl::byte>((const gsl::byte *)blob.data(), (gsl::index)blob.size()), true);
    if (!lr.is_ok()) {
        logf_("LOAD FAIL %s (nncase)", path);
        delete it;
        return false;
    }
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
    g_cache[{stem, S}] = e;
    g_order.push_back({stem, S});
    logf_("loaded %s in=[1,%d,%d,%d] out=[1,%d,%d,%d]",
          path, e.K, e.S, e.S, e.OUT, e.S, e.S);
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

// 一次 invoke：fold → tensor → run → unfold。IO_SCALE 只在无 scale 时代生效
// （plain int8 部署无 sidecar scale；SQ 时代语义已弃）。
static bool invoke(Entry &e, const float *x, int mt, float *y) {
    const int K = e.K, OUT = e.OUT, S = e.S, MT = S * S;
    std::vector<float> inp((size_t)K * MT, 0.0f);
    for (int m = 0; m < mt; m++) {
        const float *xr = x + (size_t)m * K;
        for (int c = 0; c < K; c++)
            inp[(size_t)c * MT + m] = xr[c] * IO_SCALE;
    }
    auto itr = nncase::runtime::host_runtime_tensor::create(
        nncase::dt_float32, {1, (size_t)K, (size_t)S, (size_t)S},
        gsl::as_writable_bytes(gsl::make_span(inp)),
        true,   // copy=true：M1 验证过的保真路径
        nncase::runtime::host_runtime_tensor::pool_cpu_only);
    if (!itr.is_ok()) return false;
    if (!e.interp->input_tensor(0, itr.unwrap()).is_ok()) return false;
    if (!e.interp->run().is_ok()) return false;
    auto otr = e.interp->output_tensor(0);
    if (!otr.is_ok()) return false;
    auto th = otr.unwrap().to_host();
    if (!th.is_ok()) return false;
    auto omr = nncase::runtime::host_runtime_tensor::map(th.unwrap(),
                                                         nncase::runtime::map_read);
    if (!omr.is_ok()) return false;
    const float *src = (const float *)omr.unwrap().buffer().data();
    const float inv = 1.0f / IO_SCALE;
    for (int m = 0; m < mt; m++) {
        float *yr = y + (size_t)m * OUT;
        for (int o = 0; o < OUT; o++)
            yr[o] = src[(size_t)o * MT + m] * inv;
    }
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
    if (mt == 0 || mt > 65536) { goto err; }
    {
        int S = (mt == 1) ? 1 : (mt <= 16 ? 4 : 16);
        Entry *e = get_entry(stem, S);
        if (!e) {
            logf_("ERR %s: no kmodel S%d (cap=%d)", stem, S, cap_for(S));
            goto err;
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
        if (!ok) {
            g_st.errs++;
            logf_("ERR %s: invoke failed", stem);
            goto err;
        }
        uint32_t hdr[2] = {MAGIC, 0};
        if (send(conn, hdr, 8, MSG_NOSIGNAL) != 8 ||
            send(conn, y.data(), y.size() * 4, MSG_NOSIGNAL) != (ssize_t)(y.size() * 4)) {
            logf_("send fail (client gone)");
        }
        if (g_st.calls % 50 == 0)
            logf_("calls=%ld avg=%lldus errs=%ld", g_st.calls, g_st.us / g_st.calls, g_st.errs);
        continue;   // 长连接：本请求完成，读下一个
    }
err:
    uint32_t hdr[2] = {MAGIC, 1};
    send(conn, hdr, 8, MSG_NOSIGNAL);
    }
}

// ---- warm：全量加载到 cap，逐个试跑钉 CMA（Python 版两段式语义）----
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
    auto t0 = std::chrono::steady_clock::now();
    for (int i = 0; i < n; i++) {
        Entry *e = get_entry(files[i], 16);
        if (!e) continue;
        std::vector<float> dummy((size_t)e->K * e->S * e->S, 0.0f);
        std::vector<float> y((size_t)e->OUT * e->S * e->S);
        if (invoke(*e, dummy.data(), e->S * e->S, y.data())) ok++;
        else logf_("WARMRUN FAIL %s", files[i].c_str());
    }
    char cmaline[512] = {0};
    FILE *mf = fopen("/proc/meminfo", "r");
    if (mf) {
        char line[256];
        while (fgets(line, sizeof line, mf))
            if (!strncmp(line, "CmaFree:", 8)) {
                snprintf(cmaline, sizeof cmaline, " %s", line);
                break;
            }
        fclose(mf);
    }
    logf_("warmed %d/%d S16 interpreters in %.1fs.%s", ok, n,
          std::chrono::duration<double>(std::chrono::steady_clock::now() - t0).count(),
          cmaline);
}

int main() {
    // chatd 子进程身份：父死即 TERM（PR_SET_PDEATHSIG 跨普通 exec 保留）
    prctl(PR_SET_PDEATHSIG, SIGTERM, 0, 0, 0);
    if (getppid() == 1) return 1;   // fork 与 prctl 之间父进程已死的竞态补刀

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
