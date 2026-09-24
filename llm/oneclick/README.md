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
| 1 | HF 权重导出（llm-export-old） | 本地 WSL `~/llm-export-old` | 手动（命令见 `../surgery/README.md` 锁版说明） |
| 2 | lm_head 切块（`split_lmhead_24.py`） | 本地 WSL | 手动，脚本已收 `../surgery/` |
| 3 | s1h256 静态化 + GQA 折叠（`gqa_fold_tool.py` → gqaS3） | 本地 WSL | 手动，脚本已收 |
| 4 | kv6 窗口图手术（kvwin_surgery v5→v6 进化版） | 本地 WSL | **脚本待回收集**（tmp/kvwin_surgery.py 是 v5，v6 当时就地改的） |
| 5 | 输出堆叠（`../compile/stack_outputs.py`）+ 校准集 calib48_diverse.npz | 服务器 | 堆叠已收；**校准生成脚本待回收集**（当时 heredoc 一次性） |
| 6 | 编译→推板→打分（本目录 `qwen25.sh`） | 服务器 | ✅ **一键** |
| 7 | 换模型复制（ernie / qwen3-0.6 同模板） | — | 排队：qwen25 跑稳后套模板 |

> 4/5 的"待回收集"= 从历史一次性脚本（k230_prj/tmp/、WSL /root）整理成带参数的
> 正式脚本并 ORT 对拍。做 ernie/qwen3 一键之前必须补上，否则换模型要重新考古。

## 产物与资产位

- 服务器：`$K/llm_kv6_stacked.kmodel`（编译产物）、`qwen25_24l_s1h256_kv6_stacked.onnx`、
  `calib48_diverse.npz`、`kv6onnx.tgz`（onnx 链条归档）
- 板子：`/mnt/data/static/llm_kv6_stacked.kmodel`、`bd_kv6q2cpu.py`、`noc_poke.sh`
- 基线数字与坑：`../README.md`、`../docs/known_issues.md`、`../board/static/README.md`
