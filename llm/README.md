# LLM 一腿（llama.cpp K230 fork）

Qwen3-0.6B-Q8_0 在 K230 上的混合推理：**prefill 大 GEMM 走 KPU/GNNE，decode GEMV 走 CPU RVV**。

## 架构

```
llama.cpp (fork: er6y/llama.cpp @ k230-v0.1)
  ├─ prefill GEMM (M>=32)  → kpu_gemm.cpp → nncase runtime → GNNE 硬件
  │    两种模式: KPU_LOCAL=1 进程内直调(现役) / KPU_DAEMON=1 socket 守护进程(备胎)
  └─ decode GEMV (M=1)     → CPU RVV Q8_0 内核 + K230_FAST 硬编码 decode 循环
```

## 关键环境变量

| 变量 | 作用 |
|---|---|
| `LD_BIND_NOW=1` | KPU 点亮必要条件（缺了静默零输出） |
| `KPU_KMODEL_DIR` | kmodel 目录（生产: /mnt/data/kpu_qwen/s4sq_q8） |
| `KPU_LOCAL=1` | 进程内 vendor runtime（Path B，cos=1.0 已验证） |
| `KPU_DAEMON=1` | socket 守护进程模式（备胎） |
| `KPU_TILES=s4` | 只用 S4 tile 集（443MB，内存安全） |
| `KPU_RESIDENT` | kmodel 驻留上限（LRU） |
| `KPU_PRELOAD=1` | mmap 模型前预加载全量 kmodel 抢干净 CMA |
| `K230_FAST=1` | 硬编码 decode（tg 2.58 t/s 的来源） |
| `KPU_DUMP=<stem>` | 抓某层输入/输出做数值对比 |
| `GNNE_QUIET=1` | 关 GNNE trace |

## 已验证数字（2026-09-02）

- decode tg32: **2.58 t/s**（K230_FAST 全杠杆）
- prefill: CPU 4.36 / KPU 本地 7.7 / 混合(pp128, llama-bench) 9.6-11.3 t/s
- 数值: 进程内 vs daemon 输出 cos=1.000000, max_abs_err=0（KPU_DUMP l0_q 对比）

## 已知问题（不阻塞，已立案）

- 退出时 heap corruption/segfault（nncase load-time 段无 unload，mmz_shim exit hook 兜底）
- llama-bench + 本地 kmodel 加载崩溃（预存，daemon 模式不受影响；验证用 llama-cli）
- LRU 驱逐不回收 nncase 段 → 每 load 泄漏 ~1.6MB CMA（长跑前必须 reboot 拿干净池）

## 目录

- `llamacpp/` — submodule，源码改动全在 fork 的 k230 分支
- `build/` — 编译部署链
- `board/` — 板上运行资产
- `kmodels/` — kmodel 编译脚本 + scale 边车
- `eval/` — 验证与测量工具
