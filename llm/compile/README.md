# compile/ — 全模型编译脚本（128 服务器侧）

脚本假设工作目录 `/ux/work/yilei.wang/k230/`（服务器）。本地 repo 只是源码保管地，
**改完要 scp 到服务器对应位置再跑**。**日常入口用 `../oneclick/qwen25.sh`**
（编译→推板→打分一条命令，本目录是它调用的零件）。

## 环境（服务器已就位）

```
PYTHONPATH=/ux/work/yilei.wang/k230/env/lib
DOTNET_ROOT=/ux/work/yilei.wang/k230/dotnet-sdk
PATH=$DOTNET_ROOT:$PATH
NNCASE_PLUGIN_PATH=/ux/work/yilei.wang/k230/rebuild283_srv   # 服务器自建插件
DOTNET_gcServer=0
python=/ux/work/yilei.wang/k230/conda/bin/python
```

**坑：ssh heredoc 里 `$PATH` 写 `\$PATH`；nohup 后台任务必须 `</dev/null`，
否则 ssh 挂住。**

## 现役（顶层只放现役链）

- `kv6_stacked.py` — 入炉脚本：onnx → `llm_kv6_stacked.kmodel` ~571MB
- `push_stacked.sh` — 推板：32MB 分块 + 板端 cat 合并 + 两端 md5（~3 分钟）
- `run_kv6stack.sh` — 环境包装（oneclick 没普及前的老入口）
- `kv6_verify.py` — 编译产物 ORT/板端对拍
- `lab/` — 标本与二分实验归档（s3 系列 1 层/9 区/真层切片、bulk_compile+run_variant
  的 nopos/nokv 二分、mini 单算子、kv6_q2cpu 前代现役版）。**只有复现历史实验才进来。**

输入 `qwen25_24l_s1h256_kv6_stacked.onnx` / 校准集 `calib48_diverse.npz` 从哪来：
见 `../onnx/README.md` 链条表。

## 编译要点（kv6_stacked.py 内含）

- 5 输入 decode 静态形状 [x(1,1,896), mask(1,1,1,33), pos(1,1)i32, kt_all(48,64,32), v_all(48,32,64)]
- PTQ：int16 激活 / uint8 权重，NoClip，use_mse_quant_w=False
- 校准 5 样本（随机干净数据），`set_tensor_data` **必须输入主序** `[list(t) for t in zip(*samples)]`
- 内存峰值 ~50-55GB RSS（服务器 128G 无压力）；上次全程 ~88min
