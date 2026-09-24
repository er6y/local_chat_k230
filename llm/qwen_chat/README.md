# qwen_chat/ — 官方 runner 补丁版（vendored，正式家底）

## 这是什么

K230 官方 LLM demo `qwen_chat` 的**补丁版完整源码**（~1.3MB，不含 3rd_party）。
它踩在 nncase 板上运行时（闭源 .a）上面，是"运行器的运行器"——我们的补丁都打在
这一层，不动 .a 本体。

## 上游从哪来（fork 调查已结案）

- 来源 = Canaan 官方下载包
  `https://download.kendryte.com/developer/k230/k230_ai_assistant/k230_ai_assistant.tgz`
  （md5 `41091fcc3e74cd88db2752b78e6f16d9`，内含 src/qwen_chat + 2GB 镜像）
- **没有公开 git 仓库**——官方只发 tgz + 社区教程（kendryte.com 问答
  《小设备也能玩大模型！K230 可以运行 Qwen2.5-0.5B 啦！》）。代码 lineage 溯源到
  wangzhaode 的 onnx-llm（llm-export 同作者），但 k230_ai_assistant 包装层无独立仓库。
- **定案（用户裁定）**：不 fork，本目录就是正式家底，随主项目 git 管理。
  将来官方若开源再转 submodule。

## 我们改了什么（相对上游 3 个文件）

| 文件 | 改动 | 为什么 |
|---|---|---|
| `src/tokenizer.cpp` | Sentencepiece::decode 加越界检查 + replace(pos,3) 笔误修正 | ERNIE 特殊 token id≥vocab_len 读堆垃圾 → 273GB bad_alloc 假案真凶 |
| `src/llm.cpp` | 插桩（CmaFree/异常面）+ remap 采样支持 + max_new_tokens 语义 | 剪枝模型采样越界、调试与生产契约 |
| `include/llm.hpp` | 对应声明 | — |

`patches/` = 三处改动对上游的 .patch diff（~4.5KB，对照/将来官方开源时用）。

## bench_kv6.cc — 最小计时运行器（ ours ）

`./bench_kv6 <kmodel> [reps] [H]`（llm.kmodel 文件名自动切官方 4 输入模式）。
已验证 C++ 278ms ≈ python 268ms，宿主语言无税。

## 3rd_party（63MB 预编译库，**不进 git**）

编译要链闭源运行时 .a（2025-02-05 版）：libNncase.Runtime.Native.a、
libnncase.rt_modules.k230.a、libfunctional_k230.a、libmmz.a。
获取：WSL `/root/qwen_chat_dbg/3rd_party/`（完整可编译副本），或重新解包上游 tgz。

## 交叉编译（本机 WSL1 Ubuntu-22.04，root）

```bash
cd /root/qwen_chat_dbg && export PATH=/root/xuantie/bin:$PATH
# gsl 头要落位：mkdir -p /tmp/binc/gsl && cp 3rd_party/gsl/gsl-lite.hpp /tmp/binc/gsl/
cmake -B build_riscv -DCMAKE_TOOLCHAIN_FILE=cmake/Riscv64.cmake -DDUMP_PROFILE_INFO=1
cmake --build build_riscv
```

- 工具链：`/root/xuantie`（Xuantie-900 linux-6.6 V3.0.2 gcc14.1.1，与板上内核同款）
- bench_kv6 单文件编译（不走 cmake）：
  `riscv64-unknown-linux-gnu-g++ -O2 -std=c++17 -o /tmp/bench_kv6 bench_kv6.cc \
   -I include -I 3rd_party/nncase/riscv64/include -I /tmp/binc \
   3rd_party/nncase/riscv64/lib/libNncase.Runtime.Native.a \
   3rd_party/nncase/riscv64/lib/libnncase.rt_modules.k230.a \
   3rd_party/nncase/riscv64/lib/libfunctional_k230.a \
   3rd_party/mmz/riscv64/libmmz.a -lpthread -ldl`
- 链接告警 `svinval/svnapot/svpbmt` 无害
- **坑：hrt::create 的 dtype 分支只认 float/int32，加类型要扩 nncasewrapper.hpp**
- **坑：源码 onForward 返回 nncase::tuple，插桩勿声明成 value_t**
- **坑：demo/cli_demo.cpp 有全局 operator new 覆写（NEW_FAIL 报尺寸），stderr 无缓冲**
