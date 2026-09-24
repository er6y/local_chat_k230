# compile/ — 全模型编译脚本（128 服务器侧）

脚本假设工作目录 `/ux/work/yilei.wang/k230/`（服务器）。本地 repo 只是源码保管地，
**改完要 scp 到服务器对应位置再跑**。

## 环境（服务器已就位）

```
PYTHONPATH=/ux/work/yilei.wang/k230/env/lib
DOTNET_ROOT=/ux/work/yilei.wang/k230/dotnet-sdk
PATH=$DOTNET_ROOT:$PATH
NNCASE_PLUGIN_PATH=/ux/work/yilei.wang/k230/rebuild283_srv   # 服务器自建插件
DOTNET_gcServer=0
python=/ux/work/yilei.wang/k230/conda/bin/python
```

run_kv6srv.sh / run_kv6stack.sh 是现成包装。**坑：ssh heredoc 里 `$PATH` 写 `\$PATH`；
nohup 后台任务必须 `</dev/null`，否则 ssh 挂住。**

## 现役模型

- `qwen25_24l_s1h256_kv6_stacked.onnx`（由 `qwen25_24l_s1h256_kv6.onnx` 经 stack_outputs.py 输出堆叠而来）
- 入炉脚本：`kv6_stacked.py`（=kv6_q2cpu.py 换 onnx/输出名）
- 产物：`llm_kv6_stacked.kmodel` ~571MB，板上 `/mnt/data/static/`

## 编译要点（kv6_stacked.py 内含）

- 5 输入 decode 静态形状 [x(1,1,896), mask(1,1,1,33), pos(1,1)i32, kt_all(48,64,32), v_all(48,32,64)]
- PTQ：int16 激活 / uint8 权重，NoClip，use_mse_quant_w=False
- 校准 5 样本（随机干净数据），`set_tensor_data` **必须输入主序** `[list(t) for t in zip(*samples)]`
- 内存峰值 ~50-55GB RSS（服务器 128G 无压力）

## 推板

`push_stacked.sh`：32MB 分块 + 板端 cat 合并 + 两端 md5 对账，实测 3 分钟。
板上收货位 `/mnt/data/static/llm_kv6_stacked.kmodel`。

## 标本/二分实验

`s3_static.py`（1 层 2 输出）、`build_s3b.py`/`s3b_compile.py`（9 区小标本）、
`build_s3c2.py`/`s3c_compile.py`（**真层切片 130 节点**，92s 编译，慢区复现台）、
`bulk_compile.py` + `run_variant.sh <tag>`（nopos/nokv 二分变体）。
