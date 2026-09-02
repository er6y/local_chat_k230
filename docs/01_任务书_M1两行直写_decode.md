# 任务书 01：decode M=1 两行直写 mul_mat（目标 tg32 2.46 → ~2.75+ t/s）

> **⚠️ 2026-09-01 已实验并关闭：负结果，已回退。** 实测数据与根因分析见文末 §七。
> tg32 维持 **2.46 t/s**（回退后 bit-exact 确认，MD5 32aa72fe6393b34c6aebc79bcf248040）。
> 本文件保留作为实验记录——后续内核优化勿再走"多行共享 y load 重调度"这条路（§七有数据）。
> 可复用资产：`.tools/t_m1_4r.c` 数值验证 harness（真实 ggml 布局 + dlsym 生产 .so，6/6 PASS）。

> 写给执行模型：本文件自包含，按此执行。先读根目录 `AGENTS.md` 红线 + `00_交接书` §五坑列表。
> 板卡操作纪律（safe_run/detached/sync）全部照旧，违反会挂板。

## 一、原理与收益账

decode（M=1）时所有 196 个 GEMV 共享**同一激活向量 y**。现状 dispatcher 每个 vec_dot 调用只算 1 个权重行：

- y 的 32 个 block 每次调用都重新 load（K=1024 → 1088B/行，136 条 vle8）
- 每次调用的函数调用 + vsetvli 建立 + 循环开销 ×196 次
- 结果经 `tmp[32]` + memcpy 落 dst

**改法**：ne11==1 时一次调用处理**2 个权重行**（x, x+nb01），y 只 load 一半次数，结果直写 dst（跳过 tmp/memcpy）。

收益预估（基于 424ms/token 解剖）：
- 内核 y-load 减半 + 调用开销减半：~3c/blk × 290ms/30c ≈ 29ms
- 消 tmp/memcpy：~10-15ms
- lm_head（K=1024, N=151936, M=1）同样吃此路径：再 +~10ms
- **合计 ~40-55ms → 2.46 → 2.7-2.85 t/s**

## 二、现状代码结构（已核实，2026-09-01）

### dispatcher（ggml-cpu.c）

`ggml_compute_forward_mul_mat_one_chunk`（L1171-1259）：
- L1209-1210: `blck_0=16, blck_1=16`（行块/列块）
- L1216: `float tmp[32]`
- L1249-1251: 核心调用循环：
```c
for (int64_t ir0 = iir0; ir0 < iir0 + blck_0 && ir0 < ir0_end; ir0 += num_rows_per_vec_dot) {
    vec_dot(ne00, &tmp[ir0 - iir0], (num_rows_per_vec_dot > 1 ? 16 : 0),
            src0_row + ir0 * nb01, (num_rows_per_vec_dot > 1 ? nb01 : 0),
            src1_col, (num_rows_per_vec_dot > 1 ? src1_col_stride : 0),
            num_rows_per_vec_dot);
}
```
- L1253-1255: tmp → dst memcpy

`ggml_compute_forward_mul_mat`（L1261+）：
- L1456: `num_rows_per_vec_dot = vec_dot_num_rows`
- L1460-1462: **ne11 奇数时强制 nrc=1**（decode M=1 必走此分支）
- KPU hook 在 L1274-1283（勿动）

### 内核（arch/riscv/quants.c）

`ggml_vec_dot_q8_0_q8_0` 现有两条路径：
- nrc==2：2×2 瓦片（2 权重行 × 2 激活行，4 个点积，s[0]/s[1]/s[bs]/s[bs+1]，bs=16 是 **float 偏移**）——prefill 用
- nrc==1：v2 手写 asm 快路径（nb%4==0），decode 用。4-block unroll、每迭代 3 条 vsetvli、4 条独立 f32 累加链

## 三、实现设计

### 新内核：`ggml_vec_dot_q8_0_q8_0_m1_2r`

签名（新函数，不动原函数）：
```c
// 2 weight rows x 1 activation col, M=1 decode direct path
// s[0] = dot(x, y), s[1] = dot(x + bx, y)   (s 直接是 dst 里的两个 float)
static void ggml_vec_dot_q8_0_q8_0_m1_2r(
    int n, float * GGML_RESTRICT s,       // s 指向 dst_col[ir0]，写 s[0], s[1]
    const void * GGML_RESTRICT x,          // 权重行 ir0 (block_q8_0*)
    const void * GGML_RESTRICT x1,         // 权重行 ir0+1 (即 x + nb01)
    const void * GGML_RESTRICT y);         // 激活（同一列，所有行共享）
```

asm 结构（从 v2 nrc=1 改造，省 y 重复 load）：
- 每迭代处理 4 个 block：load 4 个 y-block（共享）+ 4 个 x-block + 4 个 x1-block
- 8 条独立 f32 累加链（x 链 4 + x1 链 4）
- 结尾：s0 = sumx * dy_sum...（Q8_0 逐块 scale：`acc = Σ (dx_i * dy_i * dot(qs_x_i, qs_y_i))`——注意每 block 两个 scale 相乘，两条链各自累积）
- 不满足 `nb % 4 == 0` 或非 RVV：回退为两次调用原 nrc=1 内核

### dispatcher 改动（ggml-cpu.c one_chunk 内）

在 L1218 循环前加 M=1 快路径分支（条件从严，逐条都要满足）：
```c
if (num_rows_per_vec_dot == 1 && ne11 == 1 && src1_cont &&
    dst 连续 (nb1 == ne0*sizeof(float)) && type == GGML_TYPE_Q8_0 &&
    ne01 % 2 == 0 && 单线程 (params->ith == 0 && nth == 1)) {
    // 两行直写：ir0 步进 2，直写 dst_col
    for (int64_t ir0 = ir0_start; ir0 < ir0_end; ir0 += 2) {
        vec_dot_m1_2r(ne00, &dst_col[ir0], src0_row + ir0*nb01,
                      src0_row + (ir0+1)*nb01, src1_col);
    }
    return;  // 跳过 tmp/memcpy 路径
}
```
（ne01 奇数时尾行回退原 nrc=1 单行路径。）

### 注册方式

不进 type_traits（避免影响通用路径）——直接在 one_chunk 里 `#if defined(__riscv)` 分支内 extern 声明调用。KPU build 不受影响（KPU hook 在更外层，decode M=1 走 CPU 路径才会到这里）。

## 四、血泪契约（历史翻车点，逐条写死）

1. **block_q8_0 布局 = `{ggml_half d; int8_t qs[QK8_0]}`：d 在 offset 0，qs 在 +2，块 34 字节**。asm 里 pw=qs 指针（base+2），d 用 `flh` **负偏移 -2**。自定义结构体 qs 在前的布局（bench_q8gemv.c）与 ggml 相反，勿混。
2. **nrc=2 契约是 2×2 瓦片**（2 权重行 × 2 激活行 = 4 点积），不是 2 点积。新 m1_2r 是 2×1（2 点积）——别复用 nrc=2 代码路径，单独写。
3. **RVV 向量 load 无立即数偏移**（只有 `(rs1)` 形式）→ 子地址必须标量 addi 预计算。
4. **vsetivli AVL 立即数最大 31**；AVL=32 用 `li t6,32 + vsetvli zero,t6`。
5. **prefetch.r 偏移按 32B 粒度**（T-Head 扩展）。
6. dst 写入是**两个相邻 float**（dst_col[ir0], dst_col[ir0+1]）——连续，可一次 fsd。
7. 累加链最后乘 scale 的顺序：Q8_0 逐块 `d_x*d_y`，两条链独立，最后写回**不要**共用一次 vfsw 交叉（写 s[0] 和 s[1] 两个 word）。

## 五、验证协议（顺序执行，全过才算完）

1. **PC 侧数值单测**：先在 `.tools/bench_q8gemv.c` 加 m1_2r 变体（注意其自定义布局需换算偏移），K=1024/2048/3072/4096 vs ref，rel err < 1e-6。
2. **编译部署**：唯一正确路径见交接书 §4.4（WSL + deploy_test.sh；.so 必须 ~8.1MB，800KB=无 KPU 版本，中招重配）。
3. **配对法质量**（`.tools/qtest3.sh` 逻辑）：同一提示词 ±"，"覆盖奇偶 M——**M=2/偶数必测**（历史上 nrc=2 契约错就是偶行全垃圾）；中文连贯、无"？"、无乱码。6/6 过。
4. **性能**：`llama-bench -t 1 -p 64 -n 32 -r 3`：
   - tg32 目标 ≥ 2.6（理想 2.7+）
   - pp64 应不变（4.20 ± 0.05，prefill 走 nrc=2 未动）
   - 若 tg32 < 2.46（回退）→ 检查 dispatcher 分支条件是否误伤
5. **混合模式**：daemon 起好（p1_start_daemon.sh + 等预热），pp128 应 ~10.8 不变，中文质量过。
6. 日志全落 `/mnt/data/*.log`，safe_run 包裹，detached 启动。

## 六、预期与止损

- 预期 tg32 2.7-2.85；若实测 < 2.6，用 `/tmp/cpu_gemm_timing.log`（CPU_GEMM_TIMING=1）对比 196 GEMV 段是否缩短，定位是内核没赚还是 dispatcher 分支没进
- 若 asm 两天内数值对不齐：降级方案 = 不写新 asm，dispatcher 层做 y 指针提升 + 2 次调用原 v2 内核 + 直写 dst（省 memcpy 和一半调用开销，无 y-load 复用，预期也有 ~15ms）

## 七、实验记录（2026-09-01，负结果，已回退）

### 实测数据

| 调度 | tg32（board bench） | 微基准 ns/blkrow（K=1024, N=2048, L1 热） |
|---|---|---|
| v2 基线（1 行 × 4 块 unroll，load/mul 交错） | **2.46 ± 0.02** | **19.0** |
| 变体 A：1 块 × 4 行连发 | 1.41 ± 0.01 | - |
| 变体 B：2 块 × 2 行连发 | 1.56 ± 0.01 | 29.4 |
| 变体 C：2 块 × 2 行 load/mul 交错 | - | 28.6 |

数值正确性全程 6/6 PASS（`.tools/t_m1_4r.c`：K=1024/2048/3072/4096/1056/96 vs double 参考，rel err ~1e-7，含奇数 nb 回退路径）。

### 根因结论（2026-09-02 复核修订：原文"向量吞吐是瓶颈"被自己数据推翻，已改写）

1. **方向的真实收益天花板只有 ~5-8%**（低于本任务书预估的 10-15%）：y 是 1088B 的 L1 热数据（同一 GEMV 内全部行共享，首行载入后全程命中），重复 load 的真实成本只是指令槽 + L1 延迟，在 v2 调度里大部分已被掩盖。可省项：y-load 指令槽 ~1-2c/blk-row（≈3-6%）+ 调用开销减半（≈2%）+ memcpy 消除（≈0.1%）。**完美实现也只到 2.60-2.65，到不了 2.75+**。
2. **50% 损失的机制未定论，但可排除两个假说**：
   - 排除"向量单元吞吐是约束"：变体每 blk-row 向量指令数比 v2 **少 12.5%**（14 vs 16 条/4 blk-row），若吞吐是约束应变快而非慢 50%；
   - 排除"寄存器溢出"（对 B/C）：2×2 布局只用 29/32 寄存器，无溢出仍慢 50%；变体 A（1×4）的 v6 复用串行化是独立问题。
   - **首要嫌疑：双 x 流 + 共享 y 的三指针访存结构**（x0/x1 相距 1088B 交替读，v2 是单流顺序读 + y 热流）——同一微基准下有效带宽 1.77 → 1.17 GB/s。需 perf 计数器才能定论，本实验未做。
3. 交错调度（变体 C ≈ 变体 B）排除"连发 vs 交错"调度次序因素；指令数相近但 IPC 从 ~0.5 掉到 ~0.3，损失在访存/依赖结构不在指令预算。

### 交付物

- 代码：已全部回退（dispatcher 分支、m1_2r 内核、quants.h 声明均删除；生产 .so MD5 与基线 bit-exact 一致）
- 资产保留：`.tools/t_m1_4r.c` + `build_m1test.sh`（数值验证 harness，后续任何内核改动先跑它再上板）
- tg32 最终确认：2.44 ± 0.04（与基线 2.46 在噪声内）；pp64 4.11；配对法质量 2/2；混合 pp128 10.59（579 gemms）

### 对后续优化的启示

decode 3+ t/s 的可行路径只剩（按 2026-09-01 数据重排）：
1. **lm_head 单独量化 Q6_K**（~25ms/token，模型侧改动，不碰内核）——性价比最高的下一手
2. 词表裁剪（~67ms/token，模型手术，高风险）
3. 内核 cycle 级 profiling 后再定（perf 计数器定双流损失机制；当前证据只支持"该方向天花板 ~5-8% 不值得再烧"）

未测残留（低优先级，勿主动投入）：x1 流加 prefetch.r 可能掩盖第二流延迟——但天花板 ~5-8% 摆在那，除非 profiling 证明 load 暴露是大头，否则不再开板测。
