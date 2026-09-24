# nncase fork — K230 后端模块（submodule）

本仓库的 `nncase/` 是 **git submodule** → `https://github.com/er6y/nncase.git`，
分支 `k230`。所有编译器侧优化都在 fork 里以提交维护；TTS 等其它线共用同一 fork。

## 基线与提交

- **基线钉死 v2.8.3**（tag commit `b9f09d25`，2024-05-21）——模块内部 API 对齐 2.8.3
  闭源 DLL，master 已演进会编译不过。**严禁 rebase 到新 tag。**
- `bfdfd9a8` = 我们的模块提交：`modules/Nncase.Modules.K230/`（661 文件）+
  官方式 csproj。
- submodule 固定在 `bfdfd9a8`；更新流程：fork 里提交 → push → 主仓
  `git -C nncase fetch && git -C nncase checkout <new> && git add nncase`。

## 模块来源与可信性

K230 编译后端在官方仓**闭源**（公开只有 K210/StackVM）；模块 = 闭源 2.8.3 DLL
反编译重建（ilspycmd 8.2 工程模式 → ~340 处机械错误人工修复，LdMemberToken 静态数据
用 dnfile 从 IL 回填）。

可信性验证全过：

1. mini 单算子 GEMM：重建 DLL 产物与原闭源插件 **md5 逐字节一致**
   （fork 内 A/B 复测：闭源 2.8.3 vs fork 重建，同 mini 同配方 =
   `22a95577`，2026-09-24，nncase 2.10 core + K230 插件配对）
2. 寄存器级插桩（tile 决策 dump）
3. **全模型 949MB：与原插件产物逐字节一致**（Qwen24L 4段，md5 09e47d53）
4. fork 构建：`dotnet build` 零错误（本地 WSL dotnet 7.0.410，ProjectReference
   到 v2.8.3 公开 src——v2.8.3 的 src 树本身就是 net7.0）

## 目录结构（Top 起名 Tile*Case* → FusionConvertVisitor 分派 → GnneAction → ActionToInstruct 平铺发射 → TIR → .text）

- `modules/Nncase.Modules.K230/Nncase.Passes.Rules.K230/` — **QuantToCpu.cs 在此**
- `modules/Nncase.Modules.K230/Nncase.Targets/K230Target.cs` — pass 流水注册
  （QuantToCpu 注册在 PostLoweringProcess_5）
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

## 构建（当前：本地 WSL）

```bash
# 本地 WSL (Ubuntu-22.04-WSL1, root)：dotnet 7.0.410 + warm NuGet cache
cd /root/nncase_fork   # 或主仓 submodule 目录
~/.dotnet/dotnet build modules/Nncase.Modules.K230/Nncase.Modules.K230.csproj -c Release
# 哨兵：产物 dll 换进插件目录，compile_mini.py 编 mini GEMM，
# 与闭源 2.8.3 插件同配方产物 md5 必须一致
```

csproj 要点：v2.8.3 src 树全是 net7.0（modules/K210 的 net6.0 是死配置）；
`Directory.Packages.props` 全局开了 Nullable+TreatWarningsAsErrors（反编译源必须
在本 csproj 里显式关掉）；ImplicitUsings 关（与 System.Buffer 冲突）；
GenerateAssemblyInfo 关（保留 Properties/AssemblyInfo.cs）。

## 过渡期服务器路径

- 服务器 138 没有 dotnet SDK；构建主力 = 本地 WSL。512（/root/k230re/proj82 +
  rebuild283）是同源历史快照，512 WSL 自 09-20 起卡死待恢复。
- 服务器编译生产链（rebuild283_srv 插件目录）继续可用，与 fork 产物功能等价
  （哨兵 md5 一致性已证）。迁到 fork 构建时：把 bin/Release/net7.0/
  Nncase.Modules.K230.dll 拷进 rebuild283_srv/nncase/modules/kpu/ 即可。

## 后续大项（见 llm/docs/optimization_space.md）

T2 复活（数值排查）→ 区融合（官方 1330 小区 vs 我们 740 大区的融合差距）→ 指令流优化
