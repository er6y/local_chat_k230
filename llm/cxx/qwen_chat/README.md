# qwen_chat/ — 官方 runner 补丁版（vendored）

## 这是什么

K230 官方 LLM demo `qwen_chat` 的**补丁版源码**（~1.3MB，不含 3rd_party）。
它踩在 nncase 板上运行时（闭源 .a）上面，是"运行器的运行器"——我们的补丁都打在
这一层，不动 .a 本体。**上游**：Canaan `k230_ai_assistant` 的 `src/qwen_chat`
（llm-export/onnx-llm 同作者 wangzhaode 的代码 lineage）。

## 我们改了什么（相对上游 3 个文件）

| 文件 | 改动 | 为什么 |
|---|---|---|
| `src/tokenizer.cpp` | Sentencepiece::decode 加越界检查 + replace(pos,3) 笔误修正 | ERNIE 特殊 token id≥vocab_len 读堆垃圾 → 273GB bad_alloc 假案真凶 |
| `src/llm.cpp` | 插桩（CmaFree/异常面）+ remap 采样支持 + max_new_tokens 语义 | 剪枝模型采样越界、调试与生产契约 |
| `include/llm.hpp` | 对应声明 | — |

overlay 时代的 .patch diff 存 `../qwen_chat_overlay/`（对照上游用）。

## 3rd_party（63MB 预编译库，**不进 git**）

编译要链闭源运行时 .a（2025-02-05 版）：libNncase.Runtime.Native.a、
libnncase.rt_modules.k230.a、libfunctional_k230.a、libmmz.a。
获取：WSL `/root/qwen_chat_dbg/3rd_party/`（完整可编译副本），或上游包。
交叉编译配方见 `../README.md`（bench_kv6 同款：Xuantie-900 工具链 + gsl 头）。

## 归属路线（fork 待办）

计划与 nncase 同模式：**fork 上游 → 我们的补丁变成 fork 分支上的提交 →
本目录换成 submodule**。缺一环节：上游 git 仓库地址（downloads 快照无 .git 元数据）。
若上游不公开，则本 vendored 目录就是正式家底，可整体推成 er6y 独立仓库再 submodule。
