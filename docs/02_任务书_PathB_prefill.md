# 任务书 02：Path B — vendor runtime 直接链接（目标 pp128 10.84 → 20+ t/s）

> 写给执行模型：先读根目录 `AGENTS.md` 红线 + `00_交接书` §四架构 §五坑。本任务是探索型工程，预期多轮迭代。
> 前置：任务书 01（M=1 直写）不依赖本任务，可并行。

## 一、原理与收益账

现状混合 prefill 每个瓦片经 `unix socket → python daemon → vendor nncaseruntime`，实测每 GEMM 往返 ~58-170ms（stats：k/v ~58ms、q/gate/up ~84-110ms、down ~113ms）。pp128=10.84 的主要成本就是这趟往返。

**改法**：从 k230_sdk 源码构建 vendor runtime（C++ .a/.so），直接链进 libggml-cpu.so，把 `kpu_daemon_call()` 替换为进程内 invoke。消灭 python 调度 + socket 序列化 + 双进程切换。

预期：纯 KPU 计算时间被 daemon 延迟掩盖，直接 invoke 后才能实测真实上限；目标是 20+ t/s。

## 二、现状资产与先例

- daemon 板上脚本：`/mnt/data/kpu_llm/kpu_gemm_daemon.py`（python3，先读它——它就是 vendor runtime 的正确调用序列的活文档：加载 kmodel → input_tensor 注册 → fold → sync → run → output 拷出）
- vendor runtime 板上位置：daemon 用的 `nncaseruntime` python 包底层是闭源 so；**正确 C++ 源码在 k230_sdk**（Canaan SDK，buildroot 里 face_detect 示例有链接 vendor runtime 的先例）
- 已踩平的路（勿重复）：gnne_regs 需 preseed（/dev/mem mmap 0x80400000，kpu_gemm.cpp 现有代码直接抄）、LD_BIND_NOW=1 必须、kmodel 布局 S16/S4/S1、SmoothQuant 方向 x/s + W*s、cache flush 用 kd_mpi_sys_mmz_flush_cache
- 老的本地 runtime 尝试（GitHub 静态 runtime）已判死：数据地址绑定不可修。**必须用板子自己的 vendor runtime**（数值已证正确 cos>0.98）

## 三、实施步骤（探索型，按里程碑切）

### M1：拿到能编译的 vendor runtime 源码（最难的一步，先攻关）
1. 在 k230_sdk / Canaan buildroot 源码树里定位 nncase runtime 的 C++ 实现（face_detect demo 的链接对象就是入口线索）
2. 用 Xuantie 900 系 toolchain（板上编译或 WSL 交叉，注意 SDK 自带 toolchain 版本可能不同于当前 xuantie 14.1.1——以能跑为准）编译出 .a 或 .so
3. 板上最小验证：写个 t_vendor.c/harness，dlopen + 加载一个 l0_q.kmodel + 喂 probe_x.npy + 对照 probe_y.npy，cos>0.98 才算 M1 过（判据与 selftest 相同）

### M2：替换 daemon 调用
1. `kpu_gemm.cpp` 里 `kpu_daemon_call()` 的调用点改为进程内 invoke（保留 daemon 路径做 A/B，env 切换 KPU_LOCAL=1）
2. CMA 管理**回归进程内**：kmodel 缓冲 mmz alloc、preload 400MB 门槛恢复启用（daemon 模式跳过的那套逻辑要按 local 模式走）、mmz result-free 走 nncase 自己的 allocator（交接书 §kpu_gemm 头注释里有逆向结论）
3. 单线程安全：llama.cpp CPU 后端是单线程调用，无并发问题；但注意 196 kmodel 全驻留 886MB 超池——沿用 S4 单集 443MB 方案

### M3：全链路验证
1. selftest（KPU_SELFTEST=1）七个 l0_* stem cos>0.98
2. pp128 bench：对比 10.84 基线，目标 20+
3. 配对法中文质量 6/6
4. 长时间稳定性：连续 pp128 × 10 轮无 CMA 泄漏（CmaFree 不持续下降）、无挂死（safe_run WDT 兜底）

## 四、血泪契约（直接决定成败）

1. **gnne_regs preseed 是硬前置**：/dev/k230-gnne 无 mmap callback，runtime 自己映射失败 → 静默零输出。kpu_gemm.cpp 现有 preseed 代码直接复用
2. **LD_BIND_NOW=1** 必须（运行环境）
3. **cache coherence**：CPU fold 后必须 flush（KPU 经物理地址读 DDR）；结果拷出前必须 invalidate。现有 kd_mpi_sys_mmz_flush_cache 双向调用序列就是正确答案，照抄
4. **kmodel buffer 必须 MMZ/CMA**，malloc 缓冲 + GNNE 取指 = 锁死内存总线硬挂板（历史复现两次）
5. **SmoothQuant 方向铁律 x/s + W*s**，反了 = 乱码（三次翻车）
6. **GitHub 静态 runtime 路线勿回头**（数据地址绑定 bug 不可修）；只用板子 vendor runtime
7. CMA 池脏（kill 后 mmz 段滞留）→ 必须 reboot 拿干净池，勿硬试
8. 烧卡/镜像手术一律读卡器（历史丢数据实锤）

## 五、止损线

- M1 两轮内若 SDK 源码里找不到可编译的 runtime（闭源部分超出预期）：降级方案 = 保留 daemon 架构，做 **daemon 内 batch 优化**（一次 sendmsg 打包同层 7 个 GEMM 请求，省 6/7 往返），预期 10.84 → ~14-16 t/s，工程量小得多
- 若 M1 过但 M2 数值对不齐（cos < 0.98）：先 diff python daemon 的调用序列（它是对的），逐参数对齐（input_tensor 注册方式、sync 语义、output 映射）

## 六、验收标准

- pp128 ≥ 20 t/s（连续 3 轮 ±5%）
- 中文配对质量 6/6
- selftest 7 stem cos>0.98
- 10 轮连跑 CMA 稳定、无 WDT 复位
- 交接书更新（新架构图 + 数据 + 新坑）
