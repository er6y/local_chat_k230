# K230 SoC 硬件深度手册（寄存器级·性能榨干版）

> 整合来源：官方 Datasheet（32页扫描版已转全文）、**K230 Technical Reference Manual V0.3.1**（1220页，本地 `k230_llm/downloads/01_chip_manual/`）、kendryte.com 文档站 17 篇、k230_sdk / canmv_k230 驱动源码、**nncase_kpu 编译器插件与板端 runtime 头文件**（GNNE 指令集/tiling 算法一手来源）。
> 原始素材索引见文末第 19 章。所有地址/数值均从一手来源核抄，未经证实处已标注。

---

## 目录

```
 1 芯片总览                 8  外设寄存器速查
 2 地址映射全景             9  中断系统
 3 KPU(GNNE) 深度解析 ★   10 电源与电气规格
 4 CPU 子系统              11 启动流程与软件架构
 5 时钟系统(CMU) ★位域    12 模块使用说明与最佳实践
 6 存储系统                13 安全模块
 7 多媒体管线速览          14 开发板引脚
                           15 性能榨干总 Checklist
                           16~19 附录与素材索引
```

---

## 1. 芯片总览

### 1.1 规格速览

| 项目 | 规格 |
|---|---|
| 定位 | 端侧 AI SoC（门锁/词典笔/IPC/支付/工控读码） |
| CPU1（大核） | 玄铁 C908 @ **1.6GHz**，RV64GCV（**RVV 1.0**，128bit VPU），L1 32K I + 32K D，**L2 256KB**，跑 Linux |
| CPU0（小核） | 玄铁 C908 @ **800MHz**，RV64GCB（无向量），L1 32K+32K，**L2 128KB**，跑 RT-Smart |
| KPU（NPU） | INT8/INT16，权重稀疏压缩；ResNet50 ≥70fps、MobileNetV2 560fps、YOLOv5s 38fps（INT8） |
| SRAM | **2MB KPU 专用**（@0x8000_0000，即"KPU L2"）+ **2MB 共享**（@0x8020_0000） |
| DDR | LPDDR4 双通道×16bit @3200Mbps（理论 **12.8GB/s**）；LPDDR3 32bit @2133Mbps；最大 2GB |
| ISP | 8MP@30fps，3 摄并发（MCM），DOL2/HDR |
| 编解码 | H.264/H.265/JPEG，enc 8MP@20fps，dec 8MP@40fps，JPEG 至 8192×8192 |
| 显示 | VO 1080P@60，4 视频+8 OSD+1 背景层，MIPI DSI 4-lane |
| GPU | 2.5D GPU（VGLite 体系：命令链 DMA/光栅化 16x AA/纹理 4texel/周期） |
| AI 配套 | AI2D（Affine/Crop/Resize/Pad/Shift + CSC）、FFT 4096 点 INT16、NONAI-2D（OSD/CSC） |
| 视频配套 | DPU 3D 结构光深度、DW 畸变校正、GDMA（旋转/镜像/搬运 64K×64K） |
| 外设 | UART×5、I2C×5、SPI（1 OSPI+2 QSPI）、SDIO/eMMC×2、USB2.0 OTG×2、GPIO 64+8(PMU)、PWM×6、ADC 12bit 1MS/s×6ch |
| 安全 | AES/SM4/RSA/ECC/SM2、SHA/HMAC/SM3、TRNG 160Mbps、OTP 32Kbit、安全启动 |
| 电源 | Core/CPU/KPU 0.8V（0.72–0.88）；IO 1.8/3.3V；DDR IO 1.1/1.2/1.35V |
| 封装 | 13×13mm BGA 0.65mm 间距；K230D SIP 11×11mm（内置 1Gb LPDDR4） |
| 温度 | 工作 −40~85°C（结温 −40~125°C） |
| 快启 | 3A 首帧 ≤400ms |

### 1.2 系统框图（依据官方 Figure 1-3-1 重绘）

```
┌───────────────────────────────────────────────────────────────────────────────┐
│                                K230 SoC                                       │
│                                                                               │
│  ┌──────────── AI Subsystem ────────────┐   ┌───────────── CPU Cluster ────┐ │
│  │  ┌─────────┐  ┌──────┐  ┌─────────┐  │   │ CPU1 C908 1.6GHz            │ │
│  │  │   KPU   │  │ FFT  │  │ AI 2D   │  │   │  Vector RVV1.0 L1 32K+32K   │ │
│  │  │INT8/16  │  │4096pt│  │Engine   │──┼──▶│  L2 256KB   (Linux)         │ │
│  │  │+SRAM 2MB│  │INT16 │  │Aff/Crop │  │   ├─────────────────────────────┤ │
│  │  └─────────┘  └──────┘  │Rsz/Pad  │  │   │ CPU0 C908 800MHz            │ │
│  │       ▲                 └─────────┘  │   │  L1 32K+32K L2 128KB        │ │
│  │       │ GNNE irq #173                │   │  (RT-Smart，先启动)          │ │
│  └───────┼──────────────────────────────┘   └──────────────┬──────────────┘ │
│          │ 0x8040_0000 cfg                                │ PLIC ×2        │
│  ┌───────┴──── Storage Mgmt ────────────┐   ┌─────────────┴──────────────┐ │
│  │ DDR Subsys DDR3L/LPDDR3/LPDDR4       │   │ SYSCTL: Timer×5 WDT Mailbox │ │
│  │  ×16/×32 1600/2133/2667Mbps(控制器)  │   │  TS×1 CRP                   │ │
│  │ Share Memory SRAM 2MB                │   └──────────────────────────────┘ │
│  │ SDMA/PDMA 8ch │ 2D GDMA(OSD/CSC/Rot) │   ┌─── PMU ─────────────────────┐ │
│  │ Decompression ≥400MB/s (GZIP)        │   │ RTC / CLK&POR / Power-on    │ │
│  └──────────────────────────────────────┘   └──────────────────────────────┘ │
│  ┌──────────── Multi-Media ────────────────────────────────────────────────┐ │
│  │ VI×3 (4K@60 HDR) → ISP 4K@30fps → DW 4K@30 → DownScale ×4 路           │ │
│  │ Video: H264/HEVC/JPEG Enc 4K@20fps / Dec 4K@40fps                      │ │
│  │ 3D SL Depth Engine 1080P │ VO 1080P30 → MIPI DSI 4-lane 1.5Gbps        │ │
│  │ 2.5D GPU Engine                                                         │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│  ┌── High Speed ──────────┐  ┌── Low Speed ──────┐  ┌── Security ─────────┐ │
│  │ USB2.0 OTG×2           │  │ UART×5  I2C×5     │  │ AES/SHA/RSA/ECC    │ │
│  │ SD/eMMC HC×2           │  │ PWM×6  ADC 12b    │  │ SM2/SM3/SM4        │ │
│  │  (SDR104/HS200)        │  │ GPIO×64           │  │ TRNG 160Mbps       │ │
│  │ SPI OPI (DTR200M)      │  │ Codec/I2S         │  │ OTP 32KB           │ │
│  │ SPI QPI×2 (SDR100M)    │  │                   │  │                    │ │
│  └────────────────────────┘  └───────────────────┘  └────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────────┘
```

### 1.3 关键结论先行（给赶时间的你）

1. **KPU 是"程序驱动"的指令流处理器**（有 PC、指令缓存、MMU、硬件信号量 CCR），不是简单寄存器配置型 NPU —— 详见第 3 章，其完整指令集定义已在 nncase runtime 头文件中公开。
2. **KPU 唯一编程正路是 nncase**：算子 → Neutral IR → Target IR → Tiling → GNNE 指令流 → kmodel。
3. **CPU1/AI 域时钟支持奇数分频**（其他域只支持偶数分频），`ai_CLK_CFG@0x9110_0008` 可切 PLL3 独立 DVFS。
4. 共享 SRAM 2MB 的 **KPU port 优先级恒最高**；ai_clk >800MHz 时 sram_clk 必须换源。

---

## 2. 地址映射全景（TRM Table 1-2，裸机/驱动必备）

### 2.1 主映射表

| 起始地址 | 结束地址 | 模块 | 大小 | 备注 |
|---|---|---|---|---|
| 0x0000_0000 | 0x8000_0000 | **DDRC（DDR）** | 2GB | 物理内存窗口 |
| 0x8000_0000 | 0x8020_0000 | **KPU L2 Cache/SRAM** | 2MB | KPU 专用数据 SRAM |
| 0x8020_0000 | 0x8040_0000 | **SRAM（共享）** | 2MB | CPU/KPU/解压共用 |
| **0x8040_0000** | 0x8040_0800 | **KPU(GNNE) 寄存器** | 2KB | ⚠ 见 §3.5 完整寄存器文件 |
| 0x8040_0800 | 0x8040_0C00 | FFT | 1KB | AXI4 slave 配置+数据 |
| 0x8040_0C00 | 0x8040_1000 | AI 2D Engine | 2KB | ai2d 寄存器 |
| 0x8080_0000 | 0x8080_4000 | gsdma | 16KB | GDMA |
| 0x8080_4000 | 0x8080_8000 | DMA | 16KB | SDMA |
| 0x8080_8000 | 0x8080_C000 | decomp_gzip | 16KB | 硬件解压 |
| 0x8080_C000 | 0x8081_0000 | non_ai_2d | 16KB | 2D(OSD/CSC) |
| 0x9000_0000 | 0x9000_8000 | ISP | 32KB | |
| 0x9000_8000 | 0x9000_9000 | DeWarp | 4KB | |
| 0x9000_9000 | 0x9000_B000 | RX CSI | 8KB | MIPI RX |
| 0x9040_0000 | 0x9041_0000 | H264/HEVC/JPEG 编解码 | 64KB | |
| 0x9080_0000 | 0x9084_0000 | 2.5D GPU | 256KB | |
| 0x9084_0000 | 0x9085_0000 | VO | 64KB | |
| 0x9085_0000 | 0x9085_1000 | VO_cfg/DSI | 4KB | |
| 0x90A0_0000 | 0x90A0_0800 | 3D Engine (DPU) | 2KB | |

### 2.2 SYSCTL 域（0x9100_0000 起）

| 起始地址 | 结束地址 | 模块 | 大小 |
|---|---|---|---|
| 0x9100_0000 | 0x9100_0C00 | **PMU** | 3KB |
| 0x9100_0C00 | 0x9100_1000 | RTC | 1KB |
| **0x9110_0000** | 0x9110_1000 | **CMU（时钟）** | 4KB |
| 0x9110_1000 | 0x9110_2000 | RMU（复位） | 4KB |
| **0x9110_2000** | 0x9110_3000 | **BOOT（PLL/CPU 向量）** | 4KB |
| 0x9110_3000 | 0x9110_4000 | PWR（电源管理） | 4KB |
| 0x9110_4000 | 0x9110_5000 | mailbox（核间邮箱） | 4KB |
| 0x9110_5000 | 0x9110_5800 | iomux（引脚复用） | 2KB |
| 0x9110_5800 | 0x9110_6000 | timer | 2KB |
| 0x9110_6000 / 0x9110_6800 | — | wdt0 / wdt1 | 各 2KB |
| 0x9110_7000 | 0x9110_7800 | TS（温度传感器） | 2KB |
| 0x9110_7800 | 0x9110_8000 | HDI | 2KB |
| 0x9110_8000 | 0x9110_9000 | STC timer | 4KB |
| 0x9120_0000 | 0x9121_0000 | BootRom | 64KB |
| 0x9121_0000 | 0x9121_8000 | Security（加密引擎） | 32KB |

### 2.3 低速外设域（0x9140_0000 起，每项 4KB）

```
0x9140_0000 UART0      0x9140_1000 UART1      0x9140_2000 UART2
0x9140_3000 UART3      0x9140_4000 UART4      0x9140_5000 I2C0
0x9140_6000 I2C1       0x9140_7000 I2C2       0x9140_8000 I2C3
0x9140_9000 I2C4       0x9140_A000 PWM        0x9140_B000 GPIO0
0x9140_C000 GPIO1      0x9140_D000 ADC        0x9140_E000 CODEC(音频)
0x9140_F000 Audio(I2S/PDM)
0x9150_0000 USB2.0 OTG ×2   (256KB)
0x9158_0000 SD/eMMC HC ×2   (8KB)
0x9158_2000 SPI QSPI ×2     (8KB)
0x9158_4000 SPI OSPI        (4KB)
0x9158_5000 Hi_sys_config   (1KB)
0x9800_0000 DDRC 配置空间   (32MB)
0xC000_0000 SPI OPI XIP FLASH 窗口 (128MB)
0xF_0000_0000 PLIC（CPU0/CPU1 各一套，~2MB）
```

---

## 3. KPU（GNNE）深度解析 ★

### 3.1 微架构：KPU 是"指令流驱动"的专用处理器

依据 `nncase/runtime/k230/gnne.h` 的状态寄存器位域与 `gnne_instructions.hpp` 指令集，KPU 内部模块结构如下：

```
                     GNNE（K230 KPU）内部结构（依据 gnne.h status 位域 + 指令集归纳）
 ┌─────────────────────────────────────────────────────────────────────────────┐
 │  指令通路                                                                    │
 │   DDR/L2 ──▶ ICACHE(有预取,ICACHE_CFG@+0xf0) ──▶ DEC(译码,有 dec_pc)        │
 │                                                    │                        │
 │  ┌───────────────┬───────────────┬───────────────┼───────────────┐          │
 │  ▼               ▼               ▼               ▼               ▼          │
 │  LOAD 单元      STORE 单元       DM 数据搬运     PU 处理单元      MFU        │
 │  (load_pc,      (store_pc,      dm_w/_if/_psum  pu_pe/dw/        (map/      │
 │   load_que,      store_que,      /_act 四队列     act0/act1       reduce/    │
 │   load_biu)      store_biu,     (各有独立pc)     (各有独立pc)     pdp/crop/  │
 │                 store_order)                                      trans)    │
 │  ┌────────────────────────────────────────────────────────────────────┐    │
 │  │ TCU: dm_broadcast / conf_if+fetchif / conf_w+fetchw / conf_of      │    │
 │  │     pu_conf(+act) → pu_compute          ← 卷积主流水线            │    │
 │  │ dot: dm_if/of_conf + fetch_src1/src2    ← 点积(MatMul/FC)         │    │
 │  └────────────────────────────────────────────────────────────────────┘    │
 │  DSP: 栈式字节码核(gnne_dsp_opcode.def, 支持bf16/BR2) ← 非卷积算子         │
 │  MMU(MMU_CONF_WIDTH)  CCR×32 硬件信号量(fence_ccr 同步)  压缩权重解码器    │
 │  NOC rx/tx  ◀─内部互联─▶  BIU(load/store/all) ──AXI──▶ L2 SRAM / DDR      │
 │  AI2D 联动: ai2d_pc / ai2d_que / ai2d_busy（AI2D 指令与 GNNE 同流执行）    │
 └─────────────────────────────────────────────────────────────────────────────┘
   对外: GNNE_BASE=0x8040_0000, L2_BASE=0x8000_0000, IRQ#173, TIMEOUT 寄存器
```

**要点：**
- KPU 执行的是**编译期生成的 GNNE 指令流**（放 DDR/KPU-L2），`gnne_enable(pc_start, pc_end, pc_breakpoint)` 启动，结束/断点/异常停机——和 CPU 一样是取指-译码-执行模型。
- **CCR×32**：4bit 计数信号量，指令可 `CCRSET/CCRCLR/fence_ccr` 等待/释放——多模块（TCU/MFU/DSP/AI2D）流水线同步靠它，零 CPU 参与。
- **AI2D 指令混在 GNNE 指令流里**（有 ai2d_pc），前处理可与 KPU 计算自动流水。
- 异常类型：非法指令 / CCR 错误 / 超时（TIMEOUT 寄存器可配）。
- `gnne.h` 还暴露 `gnne_dump_status/pc/ccr` 调试接口（RTOS runtime 编入了它）。

### 3.2 GNNE 指令集（源自 gnne_instructions.hpp，10010 行完整规格）

**数据结构层（指令内嵌字段）：**

| 结构 | 作用（关键字段） |
|---|---|
| STRIDE_GLB | 全局寻址步进 h/c/n 各 21bit（byte） |
| CCRSET / CCRCLR | 设置/递减 CCR 信号量（ccr 6bit 索引，value 4bit） |
| QARG | 每通道量化参数 scale16/bias8/shift8 |
| MNCFG | 加减乘除取反、按位/字逻辑、round/floor/ceil、sqrt 模式 |
| MMU_CONF_WIDTH / COMPRESSED / SPARSIFIED | MMU 配置、权重压缩、稀疏标志 |
| PRECISION / QUAN_TYPE / QUAN_SIGNED | 精度与量化类型选择 |
| LOAD_ORDER / STORE_ORDER / ALIGNED / BROADCAST | 访存顺序/对齐/广播 |

**指令类（52 条，`inst_` 前缀）：**

| 组 | 指令 | 用途 |
|---|---|---|
| 控制 | nop, li, intr, end, fence, mmu_conf, fence_ccr | 立即数/中断/结束/栅栏/MMU/CCR 同步 |
| 访存 DM | loadif_config, loadif, load, loadif_compress_conf, load_compress_conf | 输入特征/权重加载（含压缩流） |
| 访存 DM | store, store_t_config, store_t, store_t_compress_conf, store_compress_conf | 结果写回（tile 化） |
| TCU 卷积 | tcu_dm_broadcast, tcu_dm_conf_if/fetchif, tcu_dm_conf_w/fetchw, tcu_dm_conf_of, tcu_pu_conf(+act), tcu_pu_compute | 卷积主流水线（配置→取数→计算） |
| TCU 点积 | tcu_dot_dm_if_conf, tcu_dot_dm_of_conf, tcu_dot_dm_fetch_src1/src2, tcu_pu_compute_dummy | MatMul/FC 点积路径 |
| MFU 函数 | mfu_mn_map_compute, mfu_mn_vmap_compute, mfu_reduce, mfu_vreduce, mfu_mn_broadcast_compute, mfu_mn_reduce | 逐元素 map/归约（向量版 vmap/vreduce） |
| MFU 配置 | mfu_mn_conf, mfu_mnop_conf, mfu_mn_conf2, mfu_mn_broadcast_conf | MN 通道配置 |
| MFU 池化/数据 | mfu_pdp_conf, mfu_pdp_src_conf, mfu_pdp_reduce, mfu_crop, mfu_memset, mfu_memcpy, mfu_trans | 池化数据通路/裁剪/填充/拷贝/转置 |

**DSP 栈式字节码（gnne_dsp_opcode.def，跑在 KPU 内的 DSP 核）：**

```
0x01-0x11 装载存储: LDC_I4/R4, LDIND_I1/I4/U1/BR2/R4, STIND_*, LDA_S, DUP, POP, LDARG(A), STR_I4(写GNNE寄存器)
0x21-0x29 算术:     NEG NOT ADD SUB MUL DIV DIV_U REM REM_U
0x41-0x4A 比较:     CLT(U) CLE(U) CEQ CGE(U) CGT(U) CNE
0x61-0x66 转换:     CONV_I1/I4/U1/U4/BR2/R4        ← 注意 BR2(bf16)
0x81-0x86 流程:     BR BR_TRUE BR_FALSE RET CALL THROW
0xA1-0xAA Tensor:   PAD_T SORT_ASC/DESC_T TRANSPOSE_T SLICE_T CONVERT_T
                    BROADCAST_T QUANTIZE_T DEQUANTIZE_T CLAMP_T
```

**浮点格式 fp24/bf24（fp24.h）：`fp24 = fp32 >> 8` 截断（24bit：1s+8e+15m），bf24 为脑浮点变体。KPU 非卷积算子的浮点路径即用它。**

### 3.3 Tiling 与算子映射（nncase 编译器如何"喂饱"KPU）

```
 ONNX/TFLite
   │ Importer
   ▼
 Neutral IR（设备无关）── Evaluator(常量折叠/PTQ校准执行器)
   │ Transform（算子融合、布局变换 NCHW↔NHWC、量化标记）
   ▼
 Target IR（K230）       ← Partition 按 ModuleType 切子图（CPU子图 / KPU子图）
   │ Tiling ★            ← gnne_tile_utils.h: tensor4d_segment{dim0..3 segment}
   │                       把 N/C/H/W 切成段(segment: start/end/length + p_h/p_w padding)
   │                       约束: KPU SRAM 2MB / L2 容量、滑窗宽≤8、AI2D SRAM 256×256
   │ Schedule             ← 数据依赖 → 计算顺序 + 静态 buffer 分配（零堆内存）
   ▼
 Codegen → GNNE 指令流（TCU/MFU/DSP 指令 + CCR 同步 + load/store）
   ▼
 kmodel（平坦模型，零拷贝加载）
   ▼
 板端 runtime: gnne_init → gnne_enable(pc_start,pc_end) → IRQ#173 → poll 等完成
```

**算子落点决策（综合 TRM Ch.4 + nncase 文档）：**

| 算子类型 | 执行单元 | 备注 |
|---|---|---|
| Conv2D/DWConv/GroupConv/Dilation/Deconv | TCU | 滑窗宽 ≤8；INT8/INT16 |
| MatMul/FC/LSTM/GRU | TCU dot 路径 | |
| ReLU 族/BN/量化 | TPU pu_conf_act / QARG | 量化参数每通道 scale16+bias8+shift8 |
| Pool/Reduce/Transpose/Crop/Memset/Memcpy/Broadcast | MFU | pdp=池化数据通路 |
| 逐元素/Sofmax 类/Slice/Convert | KPU 内 DSP（栈式字节码）或 CPU1 | DSP 支持 bf16 |
| Crop/Resize/Pad + CSC | AI2D | 与 GNNE 同指令流，自动流水 |
| Gather/复杂 element-wise | CPU（可 RVV 优化） | nncase stackvm + riscv64 优化目录 |

### 3.4 KPU 能力边界（TRM Ch.4 全量归纳）

| 类别 | 支持项 |
|---|---|
| 卷积 | 普通卷积（**滑窗最大宽度 8**，高度不限）、group、depthwise、dilation、deconv |
| 精度 | 卷积 INT8/INT16；部分非卷积算子 fp16（fp24/bf24 路径） |
| 权重 | 稀疏化+压缩（编译期离线，硬件解码器在线解压；SPARSIFIED 指令标志） |
| 矩阵/RNN | MatMul、FC、LSTM、GRU |
| 池化 | max/min/avg/add |
| 激活 | ReLU、ReLU6、LeakyReLU、PReLU、Swish/HardSwish、Mish、sigmoid |
| 数据 | Transpose、Memoryset/Copy、Upsample、Map/Broadcast、Concat、stride slice、slice padding、Reshape、Convert、Cast、Quant/Dequant、SpaceToBatch/BatchToSpace、Permute、Shuffle_channel、REDUCE |
| CPU 兜底 | element-wise、softmax、Gather（RVV/SIMD 可加速） |

性能口径：ResNet50 ≥70fps / MobileNetV2 560fps / YOLOv5s 38fps（INT8）。量化精度损失官方 <1%。

### 3.5 GNNE 顶层寄存器文件（gnne.h 完整定义，base=0x8040_0000）

| 偏移(约) | 寄存器 | 关键位域 |
|---|---|---|
| +0x00 | ICACHE_CFG（另：宏 `GNNE_ICACHE_CFG_OFFSET 0xf0` 以源码为准） | pre_pc_byte_len / pos_pc_byte_len / fet_len_when_miss / fet_len_when_pre（各 32b） |
| +0x10 | PC_CFG | start_pc / end_pc / breakpoint_pc（各 32b） |
| +0x20 | CTRL（128bit） | [64] gnne_enable, [66] cpu_intr_clr, [67] debug_mode_en, [68] gnne_cg_off, [69] gb_cpu_resume_en, [70:71] cpu_resume_mode, 高 64bit 为对应写掩码(wmask) |
| +0x30 | STATUS（128bit） | load/store/dm/pu/mfu 队列与模块状态、version[4b]、kpu_work_status[2b]、noc_rx/tx、biu 状态、exception_status[2b]、reset_status[2b]、axi_bresp_error、ai2d_que_no_empty、ai2d_busy、intr_status、**intr_num[32b]**、dm_w/if/psum/act、pu_pe/dw/act0/act1 状态 |
| +0x40 | DEC_LD_ST_MFU_PC | dec_pc / load_pc / store_pc / mfu_pc |
| +0x50 | PU_PC | pu_pc / dw_pc / act0_pc / act1_pc |
| +0x60 | DM_PC | dm_w_pc / dm_if_pc / dm_psum_pc / dm_act_pc |
| +0x70 | CCR_STATUS | ccr0..ccr31（各 4bit） |
| +0x80 | AI2D_PC | ai2d_pc_addr |
| +0x90 | TIME_OUT | time_out_value[32b] |
| +0x128 | **中断清除**（驱动实测） | 写 64bit `0x0000_0004_0000_0004`（gnne_dev.c） |
| … | CLK_GATE_SWITCH | dm_act/dm_w/dm_of/dm_if/act/dw 各自 cg 使能 |

**CTRL 功能写法（位编码，64bit 值|写掩码<<32）：**

```c
GNNE_CTRL_ENABLE_SET      = (1<<32)|(1)        // 使能
GNNE_CTRL_ENABLE_CLEAR    = (1<<32)|(0)        // 停机
GNNE_CTRL_CPU_INTR_CLEAR  = (1<<34)|(1<<2)     // 清 CPU 中断（即 +0x128 写 0x4_0000_0004 的来历）
GNNE_CTRL_DEBUG_MODE_SET  = (1<<35)|(1<<3)
GNNE_CTRL_CG_OFF_SET/CLEAR = (1<<36)|(1<<4)/(0)
GNNE_CTRL_CPU_RESUME_MODE_0..3 = (1<<39)|(1<<38)|(1<<37)|(mode bits)
```

**运行时 C API（RTOS runtime 已编入）：**
`gnne_set_base() / gnne_init() / gnne_enable(pc_start,pc_end,pc_breakpoint) / gnne_disable() / gnne_clear_cpu_intr() / gnne_resume(mode,pc) / gnne_get_status() / gnne_dump_status|pc|ccr() / gnne_get/set_time_out() / gnne_get/set_ai2d_pc()`

### 3.6 驱动与用户态链路

```
应用(C/C++) ── nncase runtime(libNncase.Runtime.Native.a 19.9MB + libnncase.rt_modules.k230.a + libfunctional_k230.a)
              └─ /dev/gnne_device (RT-Smart, gnne_dev.c)
                   ├─ ioctl LOCK/TRYLOCK/UNLOCK → kd_hardlock(HARDLOCK_KPU)  双核互斥
                   ├─ poll → IRQ#173 → irq_callback: 写 0x8040_0128 清中断 + wakeup
                   └─ ioremap(KPU_BASE, KPU+FFT+AI2D 大小连续映射)
MicroPython(CanMV) ── kpu 模块(kpu.c) ── Kpu_* 封装 ── 同上
```

### 3.7 PC 端仿真与调试资产（本地已备）

| 资产 | 路径 | 用途 |
|---|---|---|
| `k230_cmodel_cli.exe` | `tmp/k230_docs_mirror/nncase_kpu/whl/` | **KPU C 模型 CLI**：PC 上模拟 GNNE 指令执行（配合 nncase simulate API） |
| `nncase.simulator.k230.dll` | 同上 | K230 仿真后端 |
| `Nncase.Modules.K230.dll` | 同上 | 编译器 K230 模块（C#，可 ILSpy 反编译看 pass 细节） |
| `examples/user_guide/k230_simulate-ZH.ipynb` | nncase 仓库 | 官方模拟教程 |
| `gnne_instructions.hpp` | `nncase_kpu/rtos_rt/.../k230/` | 指令集完整规格（本文 §3.2 的来源） |
| `gnne_tile_utils.h` | 同上 | tiling 数据结构与约束 |

### 3.8 KPU 性能榨干清单

1. **卷积核宽 ≤8**；宽度 >8 会被拆分降效（9×9 避免）。
2. **吃满 KPU 专用 SRAM 2MB**：Tiling 由编译器决策，观察 kmodel 中 layer 落点；小模型可全 SRAM 化，完全绕开 DDR。
3. **INT8 优先**；INT16 吞吐减半；非卷积 fp 路径走 fp24/bf24，评估精度必要性。
4. **权重稀疏压缩**：训练做结构化剪枝（block sparsity），kmodel 变小 + 计算量降。
5. **不支持的算子三选一**：改网络 / CPU1 RVV（官方已优化 softmax/layer_norm/where）/ AI2D 卸载前处理。
6. **16B 对齐**所有 KPU 用的 DDR 地址（VB/MMZ 天然满足）。
7. **port0+port1 双口并行**：权重与激活分置 DDR/SRAM；避免与硬件解压并发（解压抢占共享 SRAM 组1）。
8. **AI2D 前处理流水**：让 resize/crop 进 GNNE 指令流（编译器自动，确认 ai2d 落点）。
9. **CCR 深度流水**由编译器自动插入（fence_ccr），无需手工；可用 cmodel 仿真对时序做 what-if。
10. **锁频**：推理时锁 ai_clk/CPU1 最高档（见 §5.4），避免 DVFS 抖动。

---

## 4. CPU 子系统（C908 大小核）

### 4.1 大小核对比

| 项 | CPU1（大核） | CPU0（小核） |
|---|---|---|
| 频率 | 1.6GHz（DVFS，见 §5.2 CPU1_CLK_CFG） | 800MHz |
| 指令集 | RV64GCV + RVV1.0 + T-Head 扩展 + TEE | RV64GCB（位操作/标量加密扩展） |
| 向量单元 | **128bit VPU**（VLEN=128） | 无 |
| L1 / L2 | 32KB I + 32KB D / **256KB** | 32KB I + 32KB D / 128KB |
| PLIC | 208 源 | 208 源 |
| 典型 OS | Linux / RT-Smart | RT-Smart（BOOTROM 先跑它） |

### 4.2 启动控制寄存器（BOOT 块 0x9110_2000，源码级）

```c
typedef struct sysctl_boot {              // base = 0x9110_2000
    struct { volatile uint32_t cfg0, cfg1, ctl, state; } pll[4]; // +0x00/0x10/0x20/0x30
    volatile uint32_t soc_boot_ctl;       // +0x40
    volatile uint32_t soc_glb_rst;        // +0x60  全芯片复位
    volatile uint32_t soc_rst_tim;        // +0x64
    volatile uint32_t soc_slp_tim;        // +0x68
    volatile uint32_t soc_slp_ctl;        // +0x6c
    volatile uint32_t clk_stable_tim;     // +0x70
    volatile uint32_t cpu_wakeup_tim;     // +0x74
    volatile uint32_t soc_wakeup_src;     // +0x78
    volatile uint32_t cpu_wakeup_cfg;     // +0x7c
    volatile uint32_t timer_pause_ctl;    // +0x80
    volatile uint32_t sysctl_int0_raw/_en/_state; // +0x90/0x94/0x98
    volatile uint32_t sysctl_int1_raw/_en/_state; // +0xa0/0xa4/0xa8
    volatile uint32_t sysctl_int2_raw/_en/_state; // +0xb0/0xb4/0xb8
    volatile uint32_t cpu0_hart_rstvec;   // +0x100  小核复位向量
    volatile uint32_t cpu1_hart_rstvec;   // +0x104  ★大核入口地址
    volatile uint32_t soc_sleep_mask;     // +0x118
} sysctl_boot_t;
```

裸机拉起大核：写 `0x9110_2104` = 入口地址，再经 RMU 解除 CPU1 复位。

### 4.3 RVV 实测（官方数据，Transformer Decoder 推理）

| 算子 | 无 RVV (ms) | RVV (ms) | 加速比 |
|---|---|---|---|
| softmax | 1749.61 | 25.72 | **68×** |
| where | 199.43 | 0.91 | **219×** |
| layer_norm | 5.81 | 0.97 | 6× |
| 整模型 | **1973.45** | **46.25** | **42.7×（-97.6%）** |

- RVV 优化代码位：`nncase/src/Native/src/kernels/stackvm/optimized/riscv64/`（声明 `opt_ops.h`、挂接 `tensor_ops.cpp`、CMake `optimized/CMakeLists.txt`）。
- 核心手法：`vle32.v` 批量加载 + `vfredsum.vs` 归约 + `rsqrt×mul` 替代除法。
- 理论峰值：INT8 GEMM ≈ 1.6GHz × 16lane × 2 = **51.2 GOPS**（无矩阵扩展，实测乘加效率约 50–70%）；VLEN=128 → 每条向量指令 4×fp32 / 8×fp16 / 16×int8。
- L2 256KB 分块收益明显（本项目 llama.cpp 单核 1.9 t/s 即受单核带宽限制）。

---

## 5. 时钟系统（CMU @ 0x9110_0000）

### 5.1 时钟树（sysctl_clk.h 源码级）

```
osc24m ──┬─▶ PLL0 (1.6G)  ──┬─ div2 → 800M   ─┐
         │                   ├─ div3 → 533M    │
         │                   └─ div4 → 400M    │
         ├─▶ PLL1 (2.376G) ──┬─ div2 → 1.188G │
         │                   ├─ div3 → 792M    │
         │                   └─ div4 → 594M    │
         ├─▶ PLL2 (2.667G) ──┬─ div2 → 1.3335G│
         │                   ├─ div3 → 889M    │
         │                   └─ div4 → 666.75M │
         └─▶ PLL3 (1.6G)  ──┬─ div2 → 800M    │
                             ├─ div3 → 533M    │
                             └─ div4 → 400M    │
                            （另: timer_pulse 50M）

CPU0 域:  cpu0_src = pll0_div2 → 800M；plic 400M / aclk 400M / pclk 200M
CPU1 域:  cpu1_core_clk_sel: 0=pll1_div2(1.188G) 1=pll3_oclk(1.6G) 2=pll0_oclk(1.6G)
AI 域:    ai_clk_sel: 0=pll1_div2 1=pll3_div2（DVFS 灵活源）
SRAM 时钟: 与 ai_clk 同源；ai_clk >800M 时 sram_clk 跟不上 → 必须换源
HS 域:    SD/USB/SSI AHB ← pll0_div4；ospi_core ← MUX(pll0_div2, pll2_div4)
          sd axi/base ← pll2_div4
⚠ 分频规则: cpu1_core/ai/ddrc_core/vpu 支持【奇数】分频；其余时钟仅支持偶数分频
```

### 5.2 核心寄存器位域（TRM 2.2.5 一手抄录）

**CPU0_CLK_CFG — offset 0x0，复位 0x0000_e65f**

| Bits | 访问 | 名称 | 说明 | 复位 |
|---|---|---|---|---|
| 31 | W1T | cpu0_core_clk_root_gdiv_upd | 写 1 触发配置生效 | 0x0 |
| 17:15 | RW | cpu_pclk_div | N-1 分频（0=bypass），源 pll0_oclk_div4 | 0x1 |
| 14 / 13 | RW | tdi_pclk_en / cpu0_pclk_enable | APB 时钟使能 | 1 |
| 12:10 | RW | cpu0_pliclk_div | N+1 分频 | 0x1 |
| 9 | RW | cpu0_pliclk_enable | | 1 |
| 8:6 | RW | cpu0_aclk_div | N+1 分频 | 0x1 |
| 4:1 | RW | cpu0_core_clk_root_div | **0:1/16·PLL0/2 … 15:16/16·PLL0/2**（16 档） | 0xf |
| 0 | RW | cpu0_core_clk_enable | | 1 |

**CPU1_CLK_CFG — offset 0x4，复位 0x0009_900d**

| Bits | 访问 | 名称 | 说明 | 复位 |
|---|---|---|---|---|
| 31 | W1T | cpu1_core_clk_div_upd | 写 1 生效 | 0 |
| 19 | RW | cpu1_pclk_enable | | 1 |
| 18:16 | RW | cpu1_pliclk_div | core/(N+1) | 0x1 |
| 15 | RW | cpu1_pliclk_enable | | 1 |
| 14:12 | RW | cpu1_aclk_div | core/(N+1) | 0x1 |
| 5:3 | RW | cpu1_core_clk_root_div | (N+1) 分频 | 0x1 |
| 2:1 | RW | cpu1_core_clk_sel | **0=pll1_oclk_div2，1=pll3_oclk，2=pll0_oclk** | 0x2 |
| 0 | RW | cpu1_core_clk_enable | | 1 |

**ai_CLK_CFG — offset 0x8，复位 0x0000_043d** ★KPU/CPU1 所属 AI 域

| Bits | 访问 | 名称 | 说明 | 复位 |
|---|---|---|---|---|
| 31 | W1T | ai_clk_div_upd | 写 1 生效 | 0 |
| 10 | RW | ai_aclk_enable | AI AXI 时钟使能 | 1 |
| 5:3 | RW | ai_clk_cdiv | (N+1) 分频 | 0x7 |
| 2 | RW | ai_clk_sel | **0=pll1_oclk_div2，1=pll3_oclk_div2** | 1 |
| 0 | RW | ai_clk_enable | | 1 |

（vpu_CLK_CFG @0xc 起，后续 hs_clken_cfg@0x18、hs_sdclk_cfg@0x1c、hs_spi_cfg@0x20、ls_clken_cfg0/1@0x24/0x28、uart_i2c_clkdiv_cfg@0x2c、ls_clkdiv_cfg@0x30、sysctl_clken_cfg@0x50、timer_clk_cfg@0x54、sysctl_clk_div_cfg@0x58、shrm_clk_cfg@0x5c、**ddr_clk_cfg@0x60**、sec_clk_div@0x80、usb_test@0x100、dphy_test@0x104、spi2axi@0x108；完整位域见 `trm/ch2_2_clock.txt` 3380 行提取件。）

### 5.3 CMU 寄存器速查（sysctl_clk_t 结构）

| 偏移 | 寄存器 | 偏移 | 寄存器 |
|---|---|---|---|
| 0x00 | cpu0_clk_cfg | 0x30 | ls_clkdiv_cfg |
| 0x04 | cpu1_clk_cfg | 0x50 | sysctl_clken_cfg |
| 0x08 | ai_clk_cfg | 0x54 | timer_clk_cfg |
| 0x0c | vpu_clk_cfg | 0x58 | sysctl_clk_div_cfg |
| 0x10 | pmu_clk_cfg | 0x5c | shrm_clk_cfg |
| 0x18 | hs_clken_cfg | 0x60 | ddr_clk_cfg |
| 0x1c | hs_sdclk_cfg | 0x80 | sec_clk_div |
| 0x20 | hs_spi_cfg | 0x100/0x104 | usb/dphy_test_clk_div |
| 0x24/0x28 | ls_clken_cfg0/1 | 0x108 | spi2axi_clk_div |
| 0x2c | uart_i2c_clkdiv_cfg | | |

### 5.4 变频操作要点（TRM 2.2.3 + Application Note）

1. 分频值写入后**必须置 `*_upd` 位（bit31 W1T）**才生效。
2. CPU1 与 KPU（ai_clk）**同源时可同步变频**（DVFS 一体），异源独立调；SDK 用 PLL3 做 AI 域灵活源。
3. ai_clk 升到 800MHz 以上前，先把 sram_clk 换到能跟上的源（TRM 2.2.3.10）。
4. PLL 结构（BOOT 块 +0x00/0x10/0x20/0x30）：cfg0/cfg1/ctl/state 四寄存器；`sysctl_boot_set_pll_lock()` 等锁定 API 在 sysctl_boot.c。
5. 推理场景：读回 `ai_CLK_CFG`/`CPU1_CLK_CFG` 确认未降频；锁 performance 档。

---

## 6. 存储系统

### 6.1 DDR

| 项 | 值 |
|---|---|
| 类型 | LPDDR4（2ch×16bit）/ LPDDR3（32bit）/ DDR3L |
| 速率 | LPDDR4 3200Mbps、LPDDR3 2133Mbps |
| 理论带宽 | **12.8 GB/s**（LPDDR4 满配） |
| 容量 | 最大 2GB；K230D SIP 内置 128MB |
| DDRC 配置空间 | 0x9800_0000（32MB） |
| 特性 | 乱序命令调度、QoS、2 rank、WAR/RAW 保序、PHY 内训练（PUB，<5µs 四态切换） |

### 6.2 SRAM 布局与仲裁

| 区域 | 地址 | 容量 | 仲裁 |
|---|---|---|---|
| KPU 专用（L2） | 0x8000_0000 | 2MB | KPU 独占 |
| 共享 | 0x8020_0000 | 2MB | 双 128bit AXI4 slave；组1=KPU port0+axi0/1+解压（解压闲置时 KPU port0 最高）；组2=KPU port1+axi0/1（port1 恒最高）；解压独占 768KB |

### 6.3 实例：128MB 物理内存布局（官方门锁 POC defconfig）

| 分区 | 基址 | 大小 | 用途 |
|---|---|---|---|
| QUICK_BOOT_CFG | 0x0000_0000 | 256KB | uboot 快启参数 |
| SENSOR_CFG | 0x0004_0000 | 768KB | sensor 参数 |
| IPCM | 0x0010_0000 | 1MB | 核间通讯 |
| RTT_SYS | 0x0200_0000 | 32MB | 大核 RT-Smart 系统 |
| LINUX_SYS | 0x0220_0000 | 58MB | 小核 Linux |
| MMZ / RTAPP（复用） | 0x05C0_0000 | 32MB | 多媒体 buffer / 大核 app |
| FACE_DATA | 0x07C0_0000 | 256KB | 人脸库 |
| SPECKLE | 0x07C4_0000 | 64KB | 散斑（OV9286） |
| AI_MODEL | 0x07D0_0000 | 128KB | 模型 |
| 合计 | — | 128MB | `CONFIG_MEM_TOTAL_SIZE=0x8000000` |

---

## 7. 多媒体管线速览

| 模块 | 能力 | 基址 |
|---|---|---|
| VI | 3×2lane 或 1×4+1×2lane MIPI；RAW8/10/12/14/16；HDR/时间戳/丢帧 | 0x9000_9000(RX) |
| ISP | 8MP@30；2D/3DNR；DOL2 2960×1666@60 / DOL3 1080p@90；DPCC/LSC/CCM/GAMMA/CAC | 0x9000_0000 |
| DW | 鱼眼/FOV/梯形校正，1–4x 放大，180°/360°，4PTZ | 0x9000_8000 |
| Codec | H.264 BP/MP/HP/HP10、H.265 Main/Main10、JPEG/MJPEG；4096²；enc 8MP@20 / dec 8MP@40 | 0x9040_0000 |
| VO | 13 层（4V+8OSD+1bg）；layer0 缩放、layer0/1 旋转镜像；1080P60 | 0x9084_0000 |
| DSI | 4-lane 1.5Gbps | 0x9085_0000 |
| DPU | 3D 结构光：Img_check/LCN/SAD/Post_proc/Align/Disp2depth；1280×800@30 | 0x90A0_0000 |
| 2.5D GPU | 命令链 DMA、16x AA、4texel/cycle、曲面细分、帧压缩 | 0x9080_0000 |
| AI2D | Affine/Crop/Resize/Pad/Shift + CSC（NV12/NV21/I420/NCHW/RGB/RAW16） | 0x8040_0C00 |
| NONAI-2D | OSD/CSC/描边/crop | 0x8080_C000 |
| GDMA | 旋转 90/180/270、镜像；2×1080×1280@15 + 1080×1920@30 | 0x8080_0000 |
| FFT | 4096 点 INT16，RIRI/RRRR…IIII，4096 点 <1ms | 0x8040_0800 |
| 解压 | GZIP/DEFLATE ≥400MB/s，字典 32KB，CRC32 | 0x8080_8000 |
| SDMA | 4ch 64bit AXI（ch1 outstanding 16） | 0x8080_4000 |
| PDMA | 8ch，35 外设口，64B/ch FIFO | 低速域 |

---

## 8. 外设寄存器速查

| 外设 | 数量 | 基址（步进 4KB） | 关键规格 |
|---|---|---|---|
| UART | 5 | 0x9140_0000 起 | 16550 兼容，FIFO 32×32b，分数波特率，9bit |
| I2C | 5 | 0x9140_5000 起 | 100k/400k/1M/3.4M，TX FIFO 32×32b |
| PWM | 6ch | 0x9140_A000 | APB3，任意占空比，毛刺消除 |
| GPIO | 64+8 | 0x9140_B000 / 0x9140_C000 | 双沿中断、去抖 |
| ADC | 6ch | 0x9140_D000 | 12bit，1MS/s |
| Codec | 1 | 0x9140_E000 | 24bit，8–192kHz，ALC |
| Audio | — | 0x9140_F000 | 8×PDM MIC、2×I2S 全双工 |
| USB | 2×OTG | 0x9150_0000 | HS/FS/LS，6 端点，FIFO 3072 |
| SDIO/eMMC | 2 | 0x9158_0000 | SDHCI，SDR104/HS200/HS400，CQE，ADMA3 |
| QSPI | 2 | 0x9158_2000 | 4bit 104M SDR，XIP |
| OSPI | 1 | 0x9158_4000 | 8bit SDR166M/DTR200M，XIP@0xC000_0000 |

IOMUX：0x9110_5000（2KB）。K230 引脚功能**任意映射**（区别于 K210 固定复用），明细见 PINOUT xlsx 与 TRM 12.9。

---

## 9. 中断系统

- 每核 PLIC @ 0xF_0000_0000，**208 外部源**。
- RT-Smart 侧 IRQ 号 = 16 + TRM 中断号（例：GNNE=173）。
- 完整 208 源表：`trm/ch2_4_interrupt.txt`。
- 核间：mailbox@0x9110_4000（32bit+硬件锁）+ sharefs/msgbuf（IPCM 1MB@0x0010_0000）。
- GNNE 中断处理链：IRQ#173 → `gnne_dev.c irq_callback` → 写 0x8040_0128 清中断 → 唤醒 poll 等待者。

---

## 10. 电源与电气规格

### 10.1 电源域（推荐工作条件）

| 域 | 电压（min/typ/max） | 供给 |
|---|---|---|
| VDD0P8_CORE / CPU / KPU / DDR_CORE / PLL / MIPI0P8 | 0.72 / 0.8 / 0.88 V | 数字核（KPU 独立电源域，可独立上下电+DVFS） |
| VDD1P8 / AVDD1P8_*（RTC/LDO/USB/ADC/CODEC/MIPI/VAA_DDR） | 1.62 / 1.8 / 1.98 V | 模拟 |
| VDDIO3P3_0..5 | 2.97 / 3.3 / 3.63 V | IO（6 组独立） |
| VDD1P1_DDR_IO | 1.1（LPDDR4）/ 1.2（LPDDR3）/ 1.35（DDR3L）V | DDR IO |
| VDD3P3_SD | 2.7 / 3.3 / 3.63 V | SD |

### 10.2 电源模式与 DVFS

- 5 模式：Power-on / Sleep0 / Sleep1 / Standby / Powerdown；唤醒源 GPIO/PMU/Timer。
- 每个电源域可独立 powerdown/powerup/时钟门控（PWR@0x9110_3000，详见 TRM 2.3 提取件 `trm/ch2_3_power.txt`，40 页）。
- **CPU1 与 KPU 支持 DVFS**：同 PLL 源同步变频，异源独立（§5.4）。SDK 的 `pm_domain_kpu.c` 管理 KPU 域上下电。

### 10.3 关键电气参数速记

- PLL：Fref 488k–4G，Fvco 800M–4G，锁定 ≤500 参考周期，输入抖动 ≤2%。
- MIPI D-PHY HS：VOD 140–270mV，ZOS 40–62.5Ω；LP：VOH 1.1–1.3V。
- USB：HS squelch 100–150mV；RPU 1.5k±5%。
- 绝对最大值/推荐值/GPIO 驱动全表：`datasheet_fulltext.txt`（Table 3-1-1 ~ 3-3-23 全量提取）。

---

## 11. 启动流程与软件架构

### 11.1 完整启动时序

```
上电/复位（RMU deglitch <8ms 滤毛刺）
  │
  ▼
BOOTROM（64KB@0x9120_0000）在 CPU0 上执行
  │  读 BOOT[1:0] 管脚：00=SPI Nor 01=SPI Nand 10=eMMC 11=SD
  │  OTP 安全标志=1 → 校验 U-Boot 签名，失败停机
  ▼
加载 U-Boot（快启参数区 @0x0000_0000 QUICK_BOOT_CFG 256KB）
  │  CPU0 写 0x9110_2104 (cpu1_hart_rstvec) 设大核入口 → RMU 解复位
  ▼
双系统并行起飞
  ├─ CPU0(小核): RT-Smart @0x0200_0000 (RTT_SYS)
  └─ CPU1(大核): Linux   @0x0220_0000 (LINUX_SYS)
       │
       ▼
应用加载：大核 ELF 可 romfs 进 MMZ 区（RTAPP/MMZ 复用 0x5C00_0000）
烧录模式：未从介质启动时，USB 优先、串口次之
```

### 11.2 软件架构分层（Linux+RT-Smart 双系统 SDK）

```
┌─────────────────── CPU1 大核 (Linux / 也可 RT-Smart) ───────────────────┐
│ 用户 App │ LVGL/OpenCV/OpenBLAS(官方已适配) │ 自研算子(RVV)           │
│────────── nncase runtime ────────── libNncase.Runtime.Native.a ────────│
│ little/linux: DRM显示 / devtmpfs / buildroot / debian(可选)            │
└───────────────▲───────────────────────────────────────────────────────┘
                │ IPCM 1MB + mailbox(0x9110_4000) + sharefs/msgbuf
┌───────────────┴─── CPU0 小核 (RT-Smart) ────────────────────────────────┐
│ 大核 App(C多媒体方案) │ mpp 多媒体中间件(VICAP/VENC/VO/Audio/DPU/FFT)   │
│ CDK 运行时 │ nncase rt_modules.k230 │ ai2d functional 库                │
│ rt-smart kernel + interdrv 驱动层: gnne/ai2d/sysctl(clk,pwr,rst,boot)/ │
│  pdma/gpio/uart/i2c/spi/sdio/wdt/rtc/hardlock/tsensor ...              │
└───────────────────────────────────────────────────────────────────────┘
裸机路线：BOOTROM → 自写固件直捣寄存器（本文全部寄存器即为此准备）
CanMV 路线：小核 RT-Smart + MicroPython（canmv_k230 仓库, kpu/ai2d 模块）
```

**多 SDK 形态**（kendryte 文档站四选一）：CanMV K230（MicroPython）/ RT-Smart SDK / Linux SDK / Linux+RT-Smart SDK。

### 11.3 关键仓库职责

| 仓库 | 内容 |
|---|---|
| kendryte/k230_sdk | 双系统完整 SDK：little/linux、big/rt-smart+interdrv、mpp、CDK |
| kendryte/canmv_k230 | CanMV MicroPython 固件：port/kpu、port/include/kpu |
| kendryte/nncase | 编译器+runtime 源码（K230 KPU target 在独立 whl：nncase_kpu） |
| kendryte/k230_docs | 官方文档站源（rst） |

---

## 12. 模块使用说明与最佳实践（官方 17 篇文档精华合并）

### 12.1 nncase 模型编译与部署（nncase_guide 精华）

**环境**：Ubuntu + Python3.8~3.13 + onnx/onnx-simplifier + dotnet-runtime 7.0；安装 `nncase`（主包）+ **`nncase_kpu`（K230 KPU 编译插件，本项目本地 `downloads/03_nncase/` 已有 v2.9/2.10/2.11）**。

**PC 端编译骨架**：

```python
import nncase
co = nncase.CompileOptions()
co.target = 'k230'
co.quant_type = 'int8'          # int8/int16；非卷积算子可 float16
co.input_type = 'uint8'          # 与传感器输出一致，省一次转换
co.quantize_method = 'kld'       # 或 'asymmetric'
co.dump_asm = True               # 可导出 GNNE 指令文本，对照 §3.2 指令集学习
compiler = nncase.Compiler(co)
compiler.import_onnx(model, import_opts)     # 或 import_tflite
for inp in calib_set:            # PTQ：逐个喂真实校准数据
    compiler.calculation(inputs)
compiler.use_ptq(ptq_opts)
compiler.compile()
open('model.kmodel','wb').write(compiler.gencode_tobytes())
```

**板端 C++ 部署**：

```cpp
nncase::runtime::interpreter interp;
interp.load_model(g_kmodel, sizeof(g_kmodel));
auto in = interp.input_tensor(0);      // hrt::alloc 共享内存分配
copy_frame_to(in);                      // AI2D 输出直接接入
interp.run();                           // 内部 gnne_enable + 等待 IRQ#173
auto out = interp.output_tensor(0);     → 后处理
```

**最佳实践**：
- 输入 dtype 设 `uint8` 直接对接 ISP 输出。
- 校准集必须用真实业务数据（randn 校准会自欺——本项目历史教训）。
- KLD 砍尾谨慎；验证用真实激活分布比对余弦相似度。
- SDK↔nncase 版本严格配对（官方对应表：`K230_SDK_nncase_version_correspondence`）。
- 前处理用 `ai2d_builder`（functional/ai2d/ai2d_builder.h）：resize/crop/pad/CSC 硬件完成，输出直接接 kmodel 输入。

### 12.2 内存优化（mem_opt/mem_analysis 精华）

| 手段 | 操作 |
|---|---|
| 小核 Linux 裁剪 | 模板 `k230_evb_doorlock_defconfig`（已删 IKCONFIG/KPROBES/IPV6/FTRACE/SWIOTLB，段对齐 4KB） |
| LVGL 显示内存 | ARGB4444/RGB565 + DRM buffer 减到 2 个 |
| 大核内存复用 | RTAPP 与 MMZ 复用（romfs 加载，省 32MB） |
| 堆/page 调优 | `free` + `list_page` 实测后设堆（门锁 POC 堆 4MB） |
| VB 池 | `cat /proc/umap/vb` 看 MinFree 按峰裁剪 |
| ISP | MCM 动态分配 + `dnr3_enable=0` |
| 分析工具 | ksize.py（内核）/ `cat /proc/umap/*`（模块占用地图） |

### 12.3 DDR 适配（ddr_guide 精华）

- 颗粒更换：按 `K230_lpddr3/lpddr4驱动适配指南` 改 uboot 传参与 DDRC 初始化表（配置空间 0x9800_0000）。
- LPDDR4-3200 需 PHY 训练（PUB 固件自动，四态切换 <5µs）；2D eye 诊断工具可用于信号质量定位。
- 本项目历史经验：SD 时钟降频手术可修复兼容性问题（见项目记忆），说明 DDRC/SDIO 时序裕量值得监控。

### 12.4 DMA/PDMA 使用（dma_api 精华）

- 大核 `kd_mpi_dma_alloc_buffer` 分配物理连续内存；`kd_mpi_dma_start` 提交任务（链表描述符模式）。
- SDMA 4 通道：ch0/ch1 服务解压模块，ch1 outstanding=16 独享大带宽。
- PDMA 8 通道面向低速外设（UART/SPI/I2C/Codec），4B 对齐固定地址。

### 12.5 DPU/FFT 加速器使用（dpu_api/fft_api 精华）

- DPU：`kd_mpi_dpu_init/start`，输入 IR+散斑图，输出深度/视差（int16）；1280×800@30。
- FFT：AXI4 slave 直接写参数+数据（0x8040_0800），4096 点 <1ms；RIRI/RRRR.IIII 两种数据排布。

### 12.6 启动优化（boot_opt 精华）

- 快启 defconfig + RTAPP/MMZ 复用 + QUICK_BOOT_CFG 参数区 → 3A 首帧 ≤400ms 目标。
- 大核 ELF 用 romfs 进 MMZ（只跑一次的场景）。

### 12.7 功耗管理（pm_guide/pmu_guide 精华）

- `kd_mpi_pm_set_device_status` 按域开关（KPU 域独立）；DVFS 调频走 `pm_domain` 驱动。
- PMU@0x9100_0000：RTC 独立供电域、长按检测、512bit 关机日志。

### 12.8 RVV 算子开发（rvv_opt/rvv_action 精华）

见 §4.3。添加新 RVV 算子 5 步：声明(opt_ops.h) → 实现(riscv64/) → 挂接(tensor_ops.cpp) → CMake → 提 PR。

---

## 13. 安全模块

AES128/192/256、CMAC、SM4、RSA 至 4096、ECDSA/ECC/SM2、SHA224–512、HMAC、SM3、TRNG 160Mbps@20MHz（稳定 <50µs）、OTP 32Kbit（安全启动标志）。寄存器窗口 0x9121_0000（32KB）。SDK 驱动：interdrv/cipher/*（pufs_* 系列）。

---

## 14. 开发板引脚（01Studio CanMV K230）

- 40pin 排针布局图：`CanMV K230 AI开发板/3.芯片资料&镜像&AI模型文件/引脚图定义.png`（已目视核对：含 3V3/5V/GND + D0..D21 数字 IO + UART/I2C/SPI/PWM 复用）。
- **权威引脚表**：`k230_llm/downloads/01_chip_manual/K230_PINOUT_V1.3_20250801.xlsx`（全封装 BGA 管脚 + 复用功能）。
- K230 特性：FPIOA 式任意复用（iomux@0x9110_5000），软件任意映射，无 K210 式固定复用限制。
- 原理图：`02 SCH_K230_DK-board V1.0.pdf`（DK 板）；CanMV 板见封装库 rar。

---

## 15. 性能榨干总 Checklist（全系统 15 条）

| # | 动作 | 章节 |
|---|---|---|
| 1 | 模型编译：target=k230、INT8、开稀疏、卷积宽≤8、input_type=uint8 | §12.1/§3.8 |
| 2 | 确认 KPU SRAM 2MB 被 kmodel 吃满；必要时缩模型/降分辨率全 SRAM 化 | §3.8 |
| 3 | dump_asm 导出 GNNE 指令，对照指令集检查 tiling 均衡度 | §3.2/§12.1 |
| 4 | KPU 不支持算子 → RVV 手写（参考官方 68×/219× 实测）或 AI2D 卸载 | §3.3/§4.3 |
| 5 | CPU1 锁最高频档：CPU1_CLK_CFG@0x9110_0004；ai 域锁频 @0x9110_0008 | §5.2/§5.4 |
| 6 | 大块数据 16B 对齐；热数据 <256KB 分块过 L2 | §3.8/§4.3 |
| 7 | 多媒体 buffer 全走 VB/MMZ；池按 MinFree 裁剪 | §12.2 |
| 8 | 前处理用 DW/AI2D/NONAI2D 硬件链；AI2D 进 GNNE 指令流自动流水 | §3.1/§7 |
| 9 | 大块模型/资源用解压引擎（400MB/s）搬；避免与 KPU 抢共享 SRAM 组1 | §7/§3.1 |
| 10 | 小核 Linux 裁剪 doorlock 模板；RTAPP/MMZ 复用 | §12.2 |
| 11 | 双核争 KPU 用 hardlock；核间大数据走共享 SRAM | §3.6/§6.2 |
| 12 | FFT 4096 点交硬件（<1ms） | §7 |
| 13 | PC 端 cmodel 仿真对时序 what-if，再上板 | §3.7 |
| 14 | DDR 裕量监控（2D eye）；异常时考虑降频手术（本项目先例） | §12.3 |
| 15 | 快启：QUICK_BOOT_CFG + romfs RTAPP → 3A 首帧≤400ms | §11.1/§12.6 |

---

## 16. 附录 A：GNNE 指令集字段详解示例

以 `QARG`（量化参数）与 `MNCFG`（算术配置）为例（完整 27 结构 + 52 指令见 `gnne_instructions.hpp`）：

```
QARG (64bit): scale_chan[16] | bias_chan[8] | shift_chan[8]   ← 每通道 requant 参数
MNCFG (64bit): add_sub_0..3[4] | mul_neg_0..3[4] | div_neg_0
               | binary_logic_isLogic | binary_logic_mode[2](And/Or/NOT/XOR)
               | round_mode[2](round/floor/ceil) | sqrt_mode[2]   ← MFU map 计算配置
STRIDE_GLB (64bit): stride_glb_h[21] | stride_glb_c[21] | stride_glb_n[21]  ← NCHW 步进
```

## 17. 附录 B：fp24/bf24 浮点

```
fp32: [31]s [30:23]e8 [22:0]m23
fp24: fp32 >> 8 截断 → [23]s [22:15]e8 [14:0]m15（truncate_to_fp24 直接右移 8 位）
bf24: 脑浮点变体（bf16 家族，8bit 指数保留，尾数 15bit）
KPU 非卷积算子浮点路径用 fp24/bf24；DSP 字节码有 CONV_BR2/LDIND_BR2/STIND_BR2
```

## 18. 附录 C：未找到/受限项（如实说明）

- GNNE 逐位域寄存器手册：TRM Ch.4 仅 2 页概述；本文 §3.5 寄存器文件来自 nncase runtime 头文件（一手、可靠，但个别偏移以宏 `GNNE_ICACHE_CFG_OFFSET 0xf0` 为准而非结构体顺序推算）。
- BootROM 内部实现：未公开（0x9120_0000 64KB）。
- K230 UG 单行本 PDF：官方以文档站散页提供（已全镜像 17 篇）。
- `Nncase.Modules.K230.dll` 为 C# IL，未反编译；如需 pass 级细节可用 ILSpy 自行展开。

## 19. 原始素材索引（本项目内路径）

| 素材 | 路径 |
|---|---|
| TRM 原版 PDF（1220页带文本层） | `k230_llm/downloads/01_chip_manual/K230_Technical_Reference_Manual_V0.3.1.pdf` |
| TRM 全文提取（按页）/章节切分/目录 | `tmp/k230_docs_mirror/trm/trm_full.txt`、`trm/ch*.txt`、`trm_toc.txt` |
| 官方文档站 17 篇 Markdown | `tmp/k230_docs_mirror/md/*.md` |
| 驱动源码 16 件 | `tmp/k230_docs_mirror/sources/`（gnne/sysctl_clk/boot/pwr/rst/pm_domain_kpu/kpu/ai2d…） |
| **nncase_kpu 编译器插件 + 模拟器** | `tmp/k230_docs_mirror/nncase_kpu/whl/`（k230_cmodel_cli.exe、simulator.dll、Modules.K230.dll） |
| **GNNE 指令集/tiling/gnne.h 头文件** | `tmp/k230_docs_mirror/nncase_kpu/rtos_rt/.../include/nncase/runtime/k230/` |
| nncase 主包/插件/板端 runtime 原始包 | `k230_llm/downloads/03_nncase/v2.9.0~v2.11.0/` |
| Datasheet 全文（网页版，含全部电气表） | `tmp/k230_docs_mirror/datasheet_fulltext.txt` |
| Datasheet 32 页渲染图 | `tmp/k230_datasheet_ocr/pages/page_001..032.png` |
| 引脚定义/封装库 | `k230_llm/downloads/01_chip_manual/K230_PINOUT_V1.3_20250801.xlsx` + rar |
| 板卡原理图 | `CanMV K230 AI开发板/.../03 芯片资料/02 SCH_K230_DK-board V1.0.pdf` |
| nncase 源码仓库树 | `tmp/k230_docs_mirror/nncase_tree.json`（2122 文件） |
| 官方文档站 | https://www.kendryte.com/k230/zh/dev/00_hardware/ |
| 源码仓库 | github.com/kendryte/k230_sdk · canmv_k230 · nncase · k230_docs |

---

*v2 深度版 · 2026-08-30 · 依据本地一手资料整理 · 地址数值均经 TRM/源码双重核对*
