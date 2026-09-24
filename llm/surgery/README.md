# surgery/ — ONNX 图手术

**分工原则：能进编译器的进编译器**（`compiler/proj82`，如 QuantToCpu）；
只改图结构、与编译器无关的手术放这里。全部脚本要求 ORT 对拍**逐位一致**后才准上板。

## 脚本

- `split_lmhead*.py` — lm_head 151936 通道 > GNNE 65535 上限 → 切 3 块 MatMul+Concat。
  五个变体：通用/24L/dyn/ernie/qwen3（按词表改参）。
  **铁律：内联保存 <2GiB；超限外部数据双文件；保存后 onnx.load 回读自证**（protobuf 2GiB 陷阱）。
- `gqa_fold_tool.py` — GQA 8:2 头折叠（ERNIE/Qwen3 同病通用），纯图手术零改编译器。
- （服务器侧）`compile/stack_outputs.py` — 96 个 [1,1,1,64] KV 输出 → 2 个 [48,1,1,64]
  堆叠输出（ktnew_all/vtnew_all，索引 l*2+g）。ORT 逐位一致已验。
  注意：实测对速度中性（-4ms），价值在接口收敛，生产消费端要配套改。

## 已判决不值得的手术（勿重做）

- **词表剪枝 30K**：-10.7% 体积只换 2% decode；CJK 配额修正后中文净账仍 ×1.47 慢。
  只有"必须塞小 CMA/Flash"时才重做（按语料频率选、CJK 配额、目标 60-70K）。
- **输出堆叠提速假说**：已证伪（97→3 输出速度不变），保留只为接口收敛。

## 编译器化迁移路径（能合则合）

| 手术 | 能否进编译器 | 说明 |
|---|---|---|
| split_lmhead（65535 通道上限切块） | **能** | 应做成 `../nncase` 模块里的图 pass（MatMul N>65535 → 切块+Concat），一劳永逸，新模型免手术。现留手术版因历史先行 |
| gqa_fold（GQA 头折叠） | **能** | 属 neutral-opt pass；同样历史先行 |
| stack_outputs / kv6 窗口堆叠 | **不能/不该** | 改的是图接口（host 解码循环的输入输出契约），不是优化——属模型设计，手术+ORT 对拍是对的 |

fork 集成完成后：前两项排期做成 pass，届时手术脚本降级为验证工具。
