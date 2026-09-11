// chatd.cpp — 全家桶纯保姆总管（不碰任何模型代码，永远不该崩）
//
// 设计铁律：本进程不链接 nncase/GNNE/任何推理库。GNNE 段错误是常态
// （vendor runtime 实录），chatd 必须在它崩时活着拉起它。
//
// 子进程表（启动顺序 = 2026-09-10 定版 kpu→tts→player→llm）：
//   kpud    KPU 算子守护（C++，线协议兼容 kpu_gemm_daemon.py）
//   tts     隔壁 TTS runner 二进制（chat.conf: TTS_BIN 换 TTS 不动 chatd）
//   player  播放守护（新 TTS 协议落定后可并入或替换）
//   llmd    LLM 守护
// ASR 未来加一个 CHILD 表项即可。
//
// 生命周期语义（用户拍板）：
//   chatd start    有序拉起（逐个等 READY），start 前 drop_caches
//   chatd stop     逆序 TERM → 10s 宽限 → KILL
//   chatd status   全家存活/READY 表
//   chatd restart  = stop + start
//   任何子进程异常退出 → 指数退避重启（1s 起步，30s 封顶）
//   chatd 自身被 kill -9 → 子进程靠 PR_SET_PDEATHSIG 集体陪葬（不留孤儿）；
//   注意硬杀跳过 GNNE/mmz 清理可能泄漏 → 之后建议 reboot 回收
//
// 待板调项（联调阶段做，见 HANDOVER）：
//   - READY 判据与 sh 版逐字对齐（kpu:"listening on" tts:"READY" llmd:"READY"）
//   - FIFO 持握语义与 safe_run.sh 的等价性验证
#include <sys/socket.h>
#include <sys/prctl.h>
#include <sys/wait.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
#include <signal.h>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <ctime>
#include <string>
#include <vector>
#include <map>

// ---------- chat.conf 解析（KEY=VALUE，# 注释，容忍引号） ----------
static std::map<std::string, std::string> g_conf;
static void load_conf(const char *path) {
    FILE *f = fopen(path, "r");
    if (!f) { fprintf(stderr, "[chatd] no conf %s\n", path); exit(1); }
    char line[1024];
    while (fgets(line, sizeof line, f)) {
        char *h = line;
        while (*h == ' ' || *h == '\t') h++;
        if (*h == '#' || *h == '\n' || *h == 0) continue;
        char *eq = strchr(h, '=');
        if (!eq) continue;
        *eq = 0;
        std::string k = h, v = eq + 1;
        while (!v.empty() && (v.back() == '\n' || v.back() == '\r' ||
                              v.back() == ' ')) v.pop_back();
        if (v.size() >= 2 && (v.front() == '"' || v.front() == '\'') &&
            v.back() == v.front())
            v = v.substr(1, v.size() - 2);
        g_conf[k] = v;
    }
    fclose(f);
}
static std::string cf(const std::string &k, const std::string &dflt) {
    auto it = g_conf.find(k);
    return it == g_conf.end() ? dflt : it->second;
}

// ---------- 子进程表 ----------
struct Child {
    const char *name;
    bool enabled = true;
    std::vector<std::string> argv;
    std::vector<std::pair<std::string, std::string>> env;
    std::string stdin_path;       // 空=/dev/null；FIFO 由 chatd 建并持握写端
    std::string log_path;         // stdout/stderr 重定向（/dev/null=丢弃）
    std::string ready_pat;        // 空=拉起即算 UP
    int ready_timeout_ms = 60000;
    pid_t pid = -1;
    bool up = false;
    long long restart_at_ms = 0;  // 退避到期时间（CLOCK_MONOTONIC）
    int backoff_ms = 0;
    int hold_fd = -1;             // FIFO 写端持握（替代 sleep 100000000 黑魔法）
};
static std::vector<Child> g_children;
static bool g_shutting_down = false;

static long long now_ms() {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (long long)ts.tv_sec * 1000 + ts.tv_nsec / 1000000;
}

static void logf_(const char *fmt, ...) {
    va_list ap;
    fprintf(stderr, "[chatd] ");
    va_start(ap, fmt);
    vfprintf(stderr, fmt, ap);
    va_end(ap);
    fputc('\n', stderr);
    fflush(stderr);
}

static void build_children() {
    std::string RUN = cf("RUN", "/tmp/chat");
    std::string LLMD = cf("LLMD", "/mnt/data/kpu_llm/llmd");
    std::string RUNNER = cf("TTS_BIN", cf("RUNNER", "")); // 隔壁换 TTS 改 conf 这一项
    std::string TTS_DIR = cf("TTS_DIR", "");
    bool KPU_ENABLE = cf("KPU_ENABLE", "0") == "1";

    Child kpu;
    kpu.name = "kpud";
    kpu.enabled = KPU_ENABLE;
    kpu.argv = {"/mnt/data/kpu_llm/kpud"};
    kpu.env = {
        {"KPUD_S16_DIR", cf("KPU_S16_DIR", "/mnt/data/kpu_qwen/s16b")},
        {"KPUD_CAP16", cf("KPU_CAP16", "50")},
        {"KPUD_CAP4", "0"},
        {"KPUD_CAP1", "0"},
        {"KPUD_WARM", "1"},
        {"KPUD_IO_SCALE", cf("KPU_IO_SCALE", "0.5")},
    };
    kpu.log_path = RUN + "/kpu_out.log";
    kpu.ready_pat = "listening on";
    kpu.ready_timeout_ms = 240000;                // warm 196 根实测 158s
    g_children.push_back(kpu);

    Child tts;
    tts.name = "tts";
    tts.enabled = !RUNNER.empty();
    // safe_run.sh 仅作崩溃标记包装（退出码进日志），chatd 主导重启
    tts.argv = {"/mnt/data/kpu_llm/safe_run.sh", RUN + "/tts_out.log", RUNNER,
                "--daemon", "--piper-dir=" + TTS_DIR};
    tts.env = {{"PIPER_DP_KPU", cf("TTS_DP_KPU", "1")}};
    tts.stdin_path = RUN + "/tts_in";             // FIFO；写端 chatd 持握
    tts.log_path = RUN + "/tts_out.log";
    tts.ready_pat = "READY";
    tts.ready_timeout_ms = 120000;
    g_children.push_back(tts);

    Child player;
    player.name = "player";
    player.argv = {"sh", "/mnt/data/chat/player.sh"};
    player.log_path = "/dev/null";
    g_children.push_back(player);

    Child llm;
    llm.name = "llmd";
    llm.argv = {LLMD,
                "--model", cf("LLM_MODEL", "Qwen3-0.6B-Q4_K_M.gguf"),
                "--ctx-size", cf("LLM_CTX", "2048"),
                "--n-predict", cf("LLM_PREDICT", "128"),
                "--temp", cf("LLM_TEMP", "0.7"),
                "--top-p", cf("LLM_TOP_P", "0.8"),
                "--top-k", cf("LLM_TOP_K", "20")};
    std::string llm_sys = cf("LLM_SYS", "");
    if (!llm_sys.empty()) {
        llm.argv.push_back("--system");
        llm.argv.push_back(llm_sys);
    }
    if (KPU_ENABLE) llm.argv.push_back("--kpu");
    llm.env = {
        {"LD_LIBRARY_PATH", "/mnt/data/kpu_llm"},
        {"LD_BIND_NOW", "1"},
        {"KPU_DAEMON", "1"},
        {"KPU_HYBRID", "1"},
        {"KPU_MIN_M", "32"},
        {"LLMD_TTS_FIFO", RUN + "/tts_in"},
        {"LLMD_UBATCH", cf("LLMD_UBATCH", "256")},
    };
    llm.stdin_path = RUN + "/llm_in";             // FIFO（ask.sh 写问题）
    llm.log_path = RUN + "/llm_out.log";
    llm.ready_pat = "READY";
    llm.ready_timeout_ms = 600000;                // 加载 ~4min
    g_children.push_back(llm);
}

static void drop_caches() {
    sync();
    int fd = open("/proc/sys/vm/drop_caches", O_WRONLY);
    if (fd >= 0) { ssize_t ig = write(fd, "3\n", 2); (void)ig; close(fd); }
}

static bool ready(const Child &c) {
    if (c.ready_pat.empty()) return true;
    FILE *f = fopen(c.log_path.c_str(), "r");
    if (!f) return false;
    char buf[262144];
    size_t n = fread(buf, 1, sizeof buf - 1, f);
    fclose(f);
    buf[n] = 0;
    return strstr(buf, c.ready_pat.c_str()) != nullptr;
}

static void spawn_child(Child &c) {
    if (!c.stdin_path.empty() && c.stdin_path[0] == '/') {
        if (access(c.stdin_path.c_str(), F_OK) != 0)
            mkfifo(c.stdin_path.c_str(), 0666);
        if (c.hold_fd < 0)   // 写端持握：防 runner/llmd open FIFO 阻塞
            c.hold_fd = open(c.stdin_path.c_str(), O_WRONLY | O_NONBLOCK);
    }
    pid_t pid = fork();
    if (pid < 0) { logf_("fork %s fail", c.name); return; }
    if (pid == 0) {
        // 三件套：父死信号（跨普通 exec 保留）+ 竞态自查 + 独立进程组
        prctl(PR_SET_PDEATHSIG, SIGTERM, 0, 0, 0);
        if (getppid() == 1) _exit(1);
        setpgid(0, 0);
        signal(SIGHUP, SIG_IGN);   // 替代 nohup：chatd 由 ssh 拉起时防会话 HUP 波及
        for (auto &kv : c.env) setenv(kv.first.c_str(), kv.second.c_str(), 1);
        if (!c.log_path.empty() && c.log_path != "/dev/null") {
            int lf = open(c.log_path.c_str(), O_WRONLY | O_CREAT | O_APPEND, 0644);
            if (lf >= 0) { dup2(lf, 1); dup2(lf, 2); }
        }
        int inf = c.stdin_path.empty()
                      ? open("/dev/null", O_RDONLY)
                      : open(c.stdin_path.c_str(), O_RDONLY);
        if (inf >= 0) dup2(inf, 0);
        std::vector<char *> av;
        for (auto &a : c.argv) av.push_back(const_cast<char *>(a.c_str()));
        av.push_back(nullptr);
        execvp(av[0], av.data());   // execv 不查 PATH，裸 "sh" 会 127
        _exit(127);
    }
    c.pid = pid;
    c.up = false;
    logf_("started %s pid=%d", c.name, (int)pid);
}

static void reap_and_restart() {
    long long now = now_ms();
    for (auto &c : g_children) {
        if (c.pid > 0) {
            int st = 0;
            pid_t r = waitpid(c.pid, &st, WNOHANG);
            if (r > 0) {
                int rc = WIFEXITED(st) ? WEXITSTATUS(st) : -WTERMSIG(st);
                logf_("%s exited rc=%d", c.name, rc);
                c.pid = -1;
                c.up = false;
                if (g_shutting_down) continue;
                c.backoff_ms = c.backoff_ms ? c.backoff_ms * 2 : 1000;
                if (c.backoff_ms > 30000) c.backoff_ms = 30000;
                c.restart_at_ms = now + c.backoff_ms;
                logf_("%s restart in %dms", c.name, c.backoff_ms);
            } else if (r == 0 && !c.up && !c.ready_pat.empty() && ready(c)) {
                c.up = true;
                c.backoff_ms = 0;
                logf_("%s READY", c.name);
            }
            continue;
        }
        // 死了：到退避点就拉（chatd stop 时不拉）
        if (!g_shutting_down && c.enabled && c.restart_at_ms &&
            now >= c.restart_at_ms) {
            c.restart_at_ms = 0;
            spawn_child(c);
        }
    }
}

int main(int argc, char **argv) {
    if (argc < 2) {
        fprintf(stderr, "usage: chatd start|stop|status|restart [conf]\n");
        return 2;
    }
    std::string cmd = argv[1];
    load_conf(argc > 2 ? argv[2] : "/mnt/data/chat/chat.conf");
    build_children();

    if (cmd == "status") {
        for (auto &c : g_children)
            printf("%-8s %-5s pid=%-6d ready=%d\n", c.name,
                   (c.pid > 0 && kill(c.pid, 0) == 0) ? "up" : "down",
                   (int)c.pid, (int)ready(c));
        return 0;
    }

    if (cmd == "stop" || cmd == "restart") {
        g_shutting_down = true;
        for (int i = (int)g_children.size() - 1; i >= 0; i--) {
            Child &c = g_children[i];
            if (c.pid > 0 && kill(c.pid, 0) == 0) {
                logf_("stopping %s (%d)", c.name, (int)c.pid);
                kill(c.pid, SIGTERM);
            }
        }
        for (int t = 0; t < 100; t++) {           // 10s 宽限
            bool any = false;
            for (auto &c : g_children)
                if (c.pid > 0 && kill(c.pid, 0) == 0) {
                    any = true;
                    waitpid(c.pid, nullptr, WNOHANG);
                }
            if (!any) break;
            usleep(100000);
        }
        for (auto &c : g_children)
            if (c.pid > 0 && kill(c.pid, 0) == 0) {
                logf_("%s not exiting, KILL", c.name);
                kill(c.pid, SIGKILL);
            }
        logf_("all stopped");
        if (cmd == "stop") return 0;
    }

    // start / restart
    drop_caches();                                // CMA/mmz 大块分配前提（svc 时代教训）
    signal(SIGCHLD, SIG_DFL);
    signal(SIGTERM, [](int) { _exit(0); });       // chatd 自己收 TERM：立即走人，
                                                  // 子进程由 PDEATHSIG 兜底
    for (auto &c : g_children) {
        if (!c.enabled) continue;
        spawn_child(c);
        for (int t = 0; t < c.ready_timeout_ms / 50 && !ready(c); t++)
            usleep(50000);                        // 硬顺序：上一个 READY 再拉下一个
        c.up = ready(c);
        if (!c.ready_pat.empty())
            logf_("%s %s", c.name, c.up ? "READY" : "NOT-READY (continue)");
    }
    logf_("all children started; entering supervise loop");
    while (true) {
        usleep(200000);
        reap_and_restart();
    }
}
