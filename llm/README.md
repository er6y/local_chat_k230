# LLM — K230 nncase kmodel 路线（主线）

Qwen2.5-0.5B-24L 自编译 kmodel 在 K230 上的 decode 极速线。
llama.cpp 路线已退役（`legacy/` 仅存档，不再维护）。

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
| `compiler/` | **nncase K230 后端 fork（proj82）**：闭源插件反编译重建源码，QuantToCpu 补丁已入 | 服务器 |
| `surgery/` | ONNX 图手术：lm_head 切块、GQA fold、输出堆叠（编译器搞不定的结构改动） | 本机/服务器 |
| `compile/` | 全模型编译脚本 + 服务器环境配方 + 分块推板 | 128 服务器 |
| `board/` | 生产基础设施（chatd/kpud） | 板 |
| `board/static/` | 静态 kmodel 板上计时/剖析/取证脚本 + NOC poke | 板 |
| `tools/` | GNNE ISA 逆向（spec/编码器/解码器）、kmodel 格式解析 | 通用 |
| `cxx/` | C++ 计时器 bench_kv6 + qwen_chat 补丁 overlay | 本机 WSL 交叉编译 |
| `docs/` | 对齐规格书、优化空间分析、已知问题清单 | — |
| `legacy/` | llama.cpp 线遗物（build/eval/kmodels/llmd），只读存档 | — |

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
