# local_chat_k230

01Studio CanMV K230 上的**全离线中文语音助手**：KWS → ASR → LLM → TTS，全程本地推理。

硬件：K230（大核 C908 @1.6GHz, RV64GCV, 2GB LPDDR4, Linux 6.6.36）+ GNNE/KPU NPU。

## 当前状态（2026-09-02）

LLM 一腿已达标可用：

| 指标 | 值 | 说明 |
|---|---|---|
| decode tg32 | **2.58 t/s** | K230_FAST 硬编码 decode，全部杠杆打完 |
| prefill | CPU 4.36 / KPU 本地 7.7 / 混合 9.6-11.3 t/s | KPU(GNNE) 混合推理已点亮 |
| 数值正确性 | cos=1.000000 | 进程内 runtime vs daemon 逐位一致 |

ASR / TTS / KWS：未开始（见各目录 README 规划）。

## 仓库结构

```
docs/          硬件手册、交接书、任务书（项目记忆，必读）
llm/           LLM 一腿
  llamacpp/    submodule → er6y/llama.cpp @ k230-v0.1（KPU 混合推理 fork）
  build/       编译部署链（WSL 交叉编译）
  board/       板上运行资产（safe_run、selftest、daemon、WiFi overlay）
  kmodels/     kmodel 编译脚本 + SmoothQuant scale 边车（.kmodel 二进制不进 git）
  eval/        数值验证 / 性能测量 / 微基准
pc-tools/      Windows 侧运维（串口 COM4、烧卡）
asr/ tts/ kws/ 后续模块占位
firmware/      小核固件 + 大小核 IPC 协议（屏幕/摄像头/LED 走小核）
image/         Linux 镜像定制（分区表、miniLinux 决策、烧录脚本）
```

## 大文件纪律

`.gguf` / `.kmodel` / 编译产物 / vendor runtime 二进制**一律不进 git**（见 .gitignore）。
模型分发走 GitHub Release 资产 + 下载脚本 + SHA256 清单（待建）。

## 快速上手

- 编译：`llm/build/README.md`（WSL，注意 800KB 假 .so 陷阱）
- 板上运行：`llm/board/README.md`（safe_run 纪律必读）
- 项目背景与历史数据：`docs/00_交接书_2026-09-01_性能攻坚阶段.md`
- 操作红线：`AGENTS.md`
