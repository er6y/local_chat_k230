# cxx/ — C++ 运行器与官方 demo 补丁

## bench_kv6.cc — 最小计时运行器（已验证：C++ 278ms ≈ python 268ms，宿主语言无税）

用法：`./bench_kv6 <kmodel> [reps] [H]`（llm.kmodel 文件名自动切官方 4 输入模式，H=历史深度）

交叉编译配方（**本机 WSL1 Ubuntu-22.04，root**）：

```bash
cd /root/qwen_chat_dbg && export PATH=/root/xuantie/bin:$PATH
# gsl 头要落位：mkdir -p /tmp/binc/gsl && cp 3rd_party/gsl/gsl-lite.hpp /tmp/binc/gsl/
riscv64-unknown-linux-gnu-g++ -O2 -std=c++17 -o /tmp/bench_kv6 bench_kv6.cc \
  -I include -I 3rd_party/nncase/riscv64/include -I /tmp/binc \
  3rd_party/nncase/riscv64/lib/libNncase.Runtime.Native.a \
  3rd_party/nncase/riscv64/lib/libnncase.rt_modules.k230.a \
  3rd_party/nncase/riscv64/lib/libfunctional_k230.a \
  /root/qwen_chat_dbg/3rd_party/mmz/riscv64/libmmz.a -lpthread -ldl
```

- 工具链：`/root/xuantie`（Xuantie-900 linux-6.6 V3.0.2 gcc14.1.1，与板上内核同款）
- 链的是闭源预编译 .a（2025-02-05 版）；链接告警 `svinval/svnapot/svpbmt` 无害
- **坑：hrt::create 的 dtype 分支只认 float/int32，加类型要扩 nncasewrapper.hpp**

## qwen_chat_overlay/ — 对官方 k230_ai_assistant demo 的补丁（overlay + diff）

改动三文件（上游 = `downloads/k230_ai/k230_ai_assistant/src/qwen_chat`）：
- `src/tokenizer.cpp` — **Sentencepiece::decode 越界修**（ERNIE 特殊 token id≥vocab_len 读堆垃圾
  → 273GB bad_alloc 假案真凶；`piece.replace(pos,3)` 个数/终点笔误）
- `src/llm.cpp` + `include/llm.hpp` — 插桩（CmaFree 输出、onForward 异常面）+ max_new_tokens 语义

应用方式：把 `overlay/src`、`overlay/include` 覆盖到上游树后按原 CMake 交叉编译
（`cmake -DCMAKE_TOOLCHAIN_FILE=cmake/Riscv64.cmake`）。

## 已知修复（防回归，详见 docs/known_issues.md）

- qwen_chat_remap 采样越界：重复惩罚 `scores[id]` 对剪枝模型用旧空间 id → 越界写堆 →
  terminate（已修：边界检查+跳过）
- 传 config.json **文件路径**不是目录（传目录 → json type_error 306）
- max_new_tokens 写 config.json（命令行那份不生效）
