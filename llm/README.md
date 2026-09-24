# LLM — K230 nncase kmodel 路线（主线）

Qwen2.5-0.5B-24L 自编译 kmodel 在 K230 上的 decode 极速线。
llama.cpp 路线已退役删除。

**编译器 fork = 上一级 submodule `../nncase/`（github.com/er6y/nncase 分支 `k230`，
基线钉 v2.8.3；modules/Nncase.Modules.K230，官方默认路径，TTS 线共用。
详见 `../docs/nncase-fork.md`）。**

## 现状（2026-09-24）

| 指标 | 数值 | 条件 |
|---|---|---|
| decode/步 | **268ms (python) / 278ms (C++)** | 黄金窗口 + NOC poke，kv6_stacked |
| 官方模型同 harness | 290ms (H=29) | 同板同日同 poke |
| 官方公开标尺 | ~202ms（4.9 tok/s） | 历史基准，今日环境未复现（自测 335ms） |
| 目标 | ≤250ms | 差 18ms |

**三件必知（血泪换来的）：**
1. **NOC poke 必打**（-28%）：`devmem 0x91103028 32 0x30002; devmem 0x91301d0c 32 0; devmem 0x91301d8c 32 0`。重启失效，S99chat 已内置。
2. **CMA 黄金窗口**：重启 → 等 ~230s → `ln -sf /dev/k230-gnne /dev/gnne_device` → `sync; echo 3 > /proc/sys/vm/drop_caches` → CmaFree ≥560MB 才能装大模型。**每次大装载之间必须 reboot**（CMA 不归还）。
3. **计时用干净数据**：NaN/垃圾喂入虚高 ~40%。权威脚本 `board/static/bd_kv6q2cpu.py`。

## 目录

| 目录 | 内容 | 运行地 |
|---|---|---|
| `onnx/` | **onnx 从哪来**：export.sh 导出（HF 权重→llm-export）+ 全部图手术脚本 + 复现等级表 | 本机 WSL |
| `compile/` | 现役编译+推板脚本（实验归档在 `compile/lab/`） | 128 服务器 |
| `oneclick/` | **一键流水线**：qwen25.sh 编译→推板→打分→PASS/FAIL | 128 服务器 |
| `board/` | 生产基础设施（chatd/kpud） | 板 |
| `board/static/` | 静态 kmodel 板上计时/剖析/取证脚本 + NOC poke | 板 |
| `tools/` | GNNE ISA 逆向（spec/编码器/解码器）、kmodel 格式解析 | 通用 |
| `cxx/` | C++ 计时器 bench_kv6 + qwen_chat 补丁 overlay | 本机 WSL 交叉编译 |
| `docs/` | 对齐规格书、优化空间分析、已知问题清单 | — |

## 快速上手（编译一版全模型）

```bash
# 1. 服务器（172.16.160.138, /ux/work/yilei.wang/k230/）
bash compile/run_kv6stack.sh        # ~2h，出 llm_kv6_stacked.kmodel
# 2. 推板（分块 + 逐块 md5）
bash compile/push_stacked.sh
# 3. 板上（172.16.72.255）
reboot; 等 230s; ln -sf /dev/k230-gnne /dev/gnne_device
sync; echo 3 > /proc/sys/vm/drop_caches
sh board/static/noc_poke.sh
python3 board/static/bd_kv6q2cpu.py /mnt/data/static/llm_kv6_stacked.kmodel 20
```

环境配方详见 `compile/README.md`。

## 代码归属速查（哪个改动该进哪个仓）

| 改动类型 | 归属 | 现状 |
|---|---|---|
| 编译器 pass/发射/量化规则 | `../nncase/` submodule（github.com/er6y/nncase 分支 k230，基线 v2.8.3，模块提交 bfdfd9a8） | ✅ 已推远端，构建+哨兵验证过（docs/nncase-fork.md） |
| 模型图结构（改 ONNX 本体） | `onnx/`（export+手术；部分将来可编译器化） | ✅ |
| 宿主 C++（demo/计时器） | `cxx/`（自留，后续生成备用模型直接用） | ✅ |
| 板上测量/取证 | `board/static/` + `tools/` | ✅ |
