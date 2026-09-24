# oneclick/ — 一键流水线

## qwen25（现役）

**一条命令（128 服务器上）**：

```bash
cd /ux/work/yilei.wang/k230/llm/oneclick && ./qwen25.sh all
# 分段可单独跑：compile / push / bench
# 日志：logs/qwen25_*.log
```

首次部署（Windows 侧把 llm 树 scp 上去）：

```bash
scp -r local_chat_k230/llm yilei.wang@172.16.160.138:/ux/work/yilei.wang/k230/
```

判定标准（写在脚本头部，也在这里）：
- decode mean ≤ 289ms（基线 268ms + 8% 容差）
- argmax 指纹 == 13（固定 seed 输入，数值回归即变）
- 参考系：官方模型同板同条件 290ms；官方宣称 202ms 在本板未复现

## 流水线全景（7 步，今天自动化到哪）

| # | 步骤 | 在哪跑 | 自动化 |
|---|---|---|---|
| 1 | HF 权重导出（llm-export-old） | 本地 WSL `~/llm-export-old` | 手动（`../onnx/export.sh`） |
| 2 | lm_head 切块（`split_lmhead_24.py`） | 本地 WSL | ✅ `../onnx/` 脚本化 |
| 3 | s1h256 静态化（**脚本待考古**）+ GQA 折叠（`gqa_fold_tool.py`） | 本地 WSL | ⚠️ 半缺口 |
| 4 | kv6 窗口图手术 | 本地 WSL | ⚠️ `../onnx/kvwin_surgery.py` 是 v5，v6 待反推 |
| 5 | 输出堆叠（`../onnx/stack_outputs.py`）+ 校准集 calib48_diverse.npz | 服务器 | ✅ 堆叠；❌ 校准生成脚本缺口 |
| 6 | 编译→推板→打分（本目录 `qwen25.sh`） | 服务器 | ✅ **一键** |
| 7 | 换模型复制（ernie / qwen3-0.6 同模板） | — | 排队：qwen25 跑稳后套模板 |

> 缺口明细与 TODO 见 `../onnx/README.md` 链条表。做 ernie/qwen3 一键之前必须补上，
> 否则换模型要重新考古。

## 产物与资产位

- 服务器：`$K/llm_kv6_stacked.kmodel`（编译产物）、`qwen25_24l_s1h256_kv6_stacked.onnx`、
  `calib48_diverse.npz`、`kv6onnx.tgz`（onnx 链条归档）
- 板子：`/mnt/data/static/llm_kv6_stacked.kmodel`、`bd_kv6q2cpu.py`、`noc_poke.sh`
- 基线数字与坑：`../README.md`、`../docs/known_issues.md`、`../board/static/README.md`
