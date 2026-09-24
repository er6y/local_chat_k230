# K230 编译器对齐官方 inst_opt 路线规格书（2026-09-19 深夜定稿）

## 0. 已验证的前提
- 重建 DLL 与原 2.8.3 插件字节级一致（mini kmodel md5 ba959043 相同）；源码在 512 /root/k230re/proj82（备份 /mnt/c/Users/wyl/proj82_src.tar.gz）
- 部署位 /root/k230/env/kpu-plugins/rebuild283/；改源码→`dotnet build -c Release`（4min）→cp 到 rebuild283→mini 编译 62s→sim oracle（/root/kmini/sim_mini.py vs sim_base.npy，1.1s）
- 迭代台插桩：TileConv2D.SearchGlbParameters/L1Search 写 /root/kmini/tiledbg.log

## 1. 计算单元层（PU 指令级）实测对比
| 项 | 我们 | 官方 |
|---|---|---|
| 每 PU_COMPUTE 平均 MAC | 385（=768 阵列÷2，right/left_shift 成对 psum 对半转发） | **3657（>768 物理上限 → SSR 大 shape 硬件内迭代）** |
| 指令构成/compute | SS_PACK_SHAPE×1 + PU_W_CONF×3 + DM_LOAD_W + PU_COMPUTE ≈12 条 | 循环体 64B：ADDI×3 自增 + DM_LOAD_L1 + L2_LOAD_W + DM_LOAD_W×2 + PU_W_CONF×5 + PU_COMPUTE×2 + CCR + JAL/JALR |
| shape 寄存器 | 每步重发小 shape | 循环外一次配置大 shape，循环内直接用 |
| 地址方式 | 每步 LUI+ADDI 绝对巨立即数 | 基址寄存器 + ADDI 步进（如 ADDI x22,x22,#768 = 24×32 权重 tile 步长）|
| 循环 | 无（全平铺） | JAL 回跳 ×60154 + JALR 子程序 ×3166（共享配置块）|

## 2. 模块层（.text）对比（同架构 Qwen2.5-0.5B）
| 项 | 官方 | 我们 |
|---|---|---|
| .text | 130.8MB（5.45MB/层） | 433.5MB（18MB/层） |
| 函数 | 9548 个全 <1MB | 75 个 >1MB（max 6.4MB） |
| .rdata | 523.7MB（权重重排 +2%） | 512.7MB |
| decode（板上实测） | 4.97 tok/s | 0.74 tok/s |

## 3. 变换清单（源码级实施顺序）
- **T1（已完成实验）** psum ping-pong：mini GEMM 路径本就 pp=1，非瓶颈；GLB tile k=2208 受权重 GLB 容量限制，属合理
- **T2 配置外提 + 循环化**（收益 ~2×，低风险）：TileConv2D.BuildL1Schedule 内 foreach L1 tile 的 C# 循环改为发射 JAL 循环：循环外发 SS_PACK_SHAPE/PU_FETCHIF_CONF1/2/4/OF_CONF（不变量），循环体 = ADDI 自增 + DM_LOAD_L1 + DM_LOAD_W + PU_W_CONF(变体) + PU_COMPUTE + JAL；需小寄存器分配器（基址+步长），Assembler 已有 label/JAL 重定位支持
- **T3 SSR 大 shape 单指令多 tile**（收益 ~5×，核心，中风险）：把 PU_FETCHIF_CONF3 的 rgroups 与 SS_PACK_SHAPE rh/rw 从 1×tile 配为 N×tile，让单条 PU_COMPUTE 硬件迭代 N 个权重 tile；地址靠 SSR stride 自动推进。**前置实验（去风险关键）**：手工改 mini .text 中一条 compute 的 shape 寄存器值→GNNE 模拟器跑→若 logits 仍对=模拟器证实语义→全速实施。参考模板=官方 fn0 解码（tmp/official_fn0_decoded.txt）
- **T4 权重重排对齐**（收益 ~1.2×）：ArrangeWeights 按 768B(24×32) 步长布局，配合 T3 的 stride；官方 .rdata +2% 是重排索引开销
- 验收门：每步 mini sim logits max_diff<1e-4 + 解码器结构对比；最终 Qwen2.5 4段全模型 ①与 REF283 同配方编译（重建忠实性）②体积/函数分布对齐官方 ③板上 decode 对齐 4.97 tok/s

## 4. 夜间队列（512，flock job24.lock 串行）
1. prune30k（23:20 起，~5:20 出炉）
2. compile24.py + rebuild283 → qwen25_24l_dyn.kmodel（重建忠实性全模型验证；参照已保护为 qwen25_24l_dyn_REF283.kmodel md5=09e47d53）→ 预计 ~11:30 出炉

## 5. 深夜关键发现（00:45）：官方 shape 寄存器全零 → MMU 描述符驱动
- **regtrace 实测官方 fn0**：全部 3172 条 SS_PACK_SHAPE 的 rn/rc/rh/rw 都=x0（零）——零=通配符，真实 shape 不在指令流里
- **机制重判**：官方头部 `MMU_CONF(rstart=x0, rdepth=x1=16, mmu_id=0..4)`——**tensor 形状/流配置在函数参数块的 MMU 描述符表**（运行时 .a 在 set_tensor 时构建），指令流只引用 mmu_id + 零通配 shape，硬件自行按描述符迭代 → 这才是单指令 3657 MAC 的来源
- **对 T3 的修正**：复刻= ①逆出 MMU 描述符格式（源：rt_modules_full.asm 32985 行反汇编 / libnncase.simulator.k230.sc / 对照官方 kmodel 参数块）→ ②教我们的发射器生成描述符+零通配 shape+JAL 循环。运行时 .a 已支持该机制（官方模型能跑），只需编译器侧对齐
- **好消息**：模拟器+板上 .a 都是现成的语义裁判，官方 660MB kmodel 就是黄金样例；T2（循环化+配置外提，~2×）不依赖 MMU，可先行
- 寄存器追踪工具：/root/k230re/regtrace5.py（吃解码文本，常量传播+shape 直方图）；我们的 Q24 fn0 shape 实测 ss4=(1,24,1,1)×175713 / ss1=(1,32,1,1)——每 compute 一 tile 实锤
