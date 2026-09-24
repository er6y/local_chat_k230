# nncase — K230 后端 fork（模块：Nncase.Modules.K230）

官方 nncase（github.com/kendryte/nncase）的模块官方默认位置是 `modules/Nncase.Modules.K210`
等；K230 后端**闭源**未公开——本仓库把它以官方默认路径补齐：`modules/Nncase.Modules.K230/`。
来源=闭源 2.8.3 DLL 反编译重建（ilspycmd 8.2 工程模式 → ~340 处机械错误人工修复），
**所有编译器侧优化都改在这里**。TTS 等其它线共用同一 fork。

## fork 集成状态（待办）

- 计划：fork kendryte/nncase（**基线必须钉在 2.8.3 时代**——本模块内部 API 对齐 2.8.3
  闭源 DLL，master 已演进会编译不过），把 `modules/Nncase.Modules.K230/` 覆盖进 fork、
  挂进 nncase.sln、补丁以提交形式维护，`git remote` 指到用户 fork 后推送。
- 过渡期构建仍走服务器平铺目录 `/ux/work/yilei.wang/k230/proj82`（内容=本目录同源）；
  csproj 的 HintPath 已改写为 `../../../env/lib/nncase/...`，即服务器上落到
  `/ux/work/yilei.wang/k230/nncase/modules/Nncase.Modules.K230` 即可原样构建。

备份：512 `/mnt/c/Users/wyl/proj82_src.tar.gz`（历史快照）。

## 可信性（三级验证全过）

1. mini 单算子 GEMM：重建 DLL 产物与原闭源插件 **md5 逐字节一致**（ba959043）
2. 寄存器级插桩（tile 决策 dump）
3. **全模型 949MB：与原插件产物逐字节一致**（Qwen24L 4段，md5 09e47d53）

## 目录结构（Top 起名 Tile*Case* → FusionConvertVisitor 分派 → GnneAction → ActionToInstruct 平铺发射 → TIR → .text）

- `Nncase.Passes.Rules.K230/` — **QuantToCpu.cs 在此**（我们的补丁，见下）
- `Nncase.Targets/K230Target.cs` — pass 流水注册（QuantToCpu 注册在 PostLoweringProcess_5）
- `Nncase.CodeGen.K230/` — GNNE ISA 汇编/发射层
- `Nncase.Passes.Rules.Tile/` — tile 决策（TileOptions.ForceFence 存在但无人设 true）

## 已入补丁

**QuantToCpu**（2026-09-24 上线）：小张量（<1MB f32）的独立 Quantize 改写为
mul/round/clamp/add/cast 的 stackvm CPU 原语链，整个量化区消失。
decode 全模型 390 量化区→0（-29ms）；prefill 大张量守门不动。

## 已禁用（勿随手打开）

**GnnePeephole T2**（`ActionToInstruct.cs` 里两行已注释）：
SS 去重 23.7% + ADDI 自增，mini 上 -2.6% 且 oracle 逐位过，
但**全模型 sim cos=0.9396 数值不过**——待查（可能是区边界配对变换的值域假设）。
恢复前必须先过全模型 oracle。

## 构建流程（128 服务器）

```bash
cd /ux/work/yilei.wang/k230/proj82
dotnet build Nncase.Modules.K230.csproj -c Release   # ~4min，0 警告基线
# 部署：产物 dll → rebuild283_srv/nncase/modules/kpu/Nncase.Modules.K230.dll
# 验证：python compile/mini_srv.py 编 mini GEMM，md5 应=ba959043（基线哨兵）
```

## 编译环境（服务器 /ux/work/yilei.wang/k230/）

- `env/lib/nncase/`：2.8.3 wheel 的 C# DLL（csproj 相对引用这里）
- `dotnet-sdk/`：7.0.410 项目私有
- `rebuild283_srv/`：部署插件目录（NNCASE_PLUGIN_PATH 指向）
- 配方：`PYTHONPATH=env/lib + DOTNET_ROOT=dotnet-sdk + NNCASE_PLUGIN_PATH=rebuild283_srv + DOTNET_gcServer=0`
- 坑：ssh heredoc 里 `$PATH` 要写 `\$PATH`（本地 Git Bash 会展开）

## 后续大项（见 docs/optimization_space.md）

T2 复活（数值排查）→ 区融合（官方 1330 小区 vs 我们 740 大区的融合差距）→ 指令流优化
