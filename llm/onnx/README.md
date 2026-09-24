# onnx/ — 编译输入从哪来（HF 权重 → 导出 → 图手术 → 编译输入）

**回答"onnx 是下载的还是自己导的"：权重从 HuggingFace 官方仓下载
（Qwen/Qwen2.5-0.5B-Instruct，存 WSL `/root/models/`），onnx 全部是自己导的** ——
llm-export（wangzhaode/llm-export @ b4d60fcd64，与官方 K230 demo 同款工具）从权重
现场导出，再经本目录的图手术脚本加工。onnx 产物**不进 git**（体积 0.5-2GB），
服务器归档：`/ux/work/yilei.wang/k230/kv6onnx.tgz`。

## 链条总表（qwen25 现役线）

| # | 步骤 | 脚本 | 复现等级 |
|---|---|---|---|
| 1 | HF→onnx 导出 | `export.sh`（llm-export-old，锁版 torch2.2.1/transformers4.40.2/numpy1.26.4，禁 --onnx_slim） | ✅ 脚本化 |
| 2 | lm_head 切 3 块（151936>GNNE 65535 上限） | `split_lmhead_24.py <src> <dst>`（内联保存 <2GiB 铁律） | ✅ 脚本化 |
| 3 | s1h256 静态化（seq=1/history=256 decode 图） | **llm-export 定制导出，当时就地改的，脚本待考古**（中间产物只在 512 /root/k230/input/，机器挂） | ⚠️ 缺口 |
| 4 | GQA 8:2 头折叠 | `gqa_fold_tool.py in.onnx out.onnx [--verify]`（通用版，ERNIE/Qwen3 同病） | ✅ 脚本化 |
| 5 | kv6 窗口图手术（host 管 KV 窗，5 输入契约） | `kvwin_surgery.py`（**v5 版**；v6=去 in-graph concat 的进化版待从 kv6.onnx 反推 diff） | ⚠️ 半缺口 |
| 6 | 96 个 KV 输出 → 2 个堆叠输出 | `stack_outputs.py <src> <dst>` | ✅ 脚本化 |
| 7 | 校准集 calib48_diverse.npz（真实文本前向钩子抓 KV） | **当时 heredoc 一次性，未留档** | ❌ 缺口 |

- ✅=脚本+参数齐，⚠️/❌=做 ernie/qwen3 一键前必须补（详见 TODO）。
- 每步铁律：**ORT 对拍逐位一致才准进下一步**（`gqa_fold_tool.py --verify` 是范本）。
- 2GiB 陷阱：内联保存仅限 <2GiB；超限外部数据双文件；保存后 `onnx.load` 回读自证。

## 本目录文件

- `export.sh` — 第 1 步（换模型时改 `LLM_EXPORT_TOOL` 指向对应 fork：`~/llm-export-ernie` / `~/llm-export-qwen3`，需 transformers 4.57.1）
- `split_lmhead.py|_24|_dyn|_ernie|_qwen3.py` — 各模型 lm_head 切块（词表参数不同）
- `gqa_fold_tool.py` — GQA 广播消除（Q 头数 % KV 头数 == 0 的任意模型通用）
- `kvwin_surgery.py` — kv 窗口图手术 v5（v6 差异 TODO 见上表）
- `stack_outputs.py` — 输出堆叠（接口收敛用，速度中性已证）

## TODO（按优先级）

1. **第 7 步校准生成脚本重写**：跑 float 模型抓 48 层 KV past（[1,256,2,64]）+ 干净
   x/mask/pos，多样本真实文本。没有它换模型就得手搓校准。
2. **第 3 步 s1h256 静态化考古**：从 512 /root/k230re（只读 rootfs 通道）或 llm-export
   git diff 反推当时的导出 patch，固化成 `export_static.sh`。
3. **kvwin v5→v6 diff**：对照 kv6onnx.tgz 里的 kv6.onnx 与 v5 产物反推，升级脚本。
