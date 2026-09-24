# 编译部署链（WSL 交叉编译）

工具链：Xuantie GCC 14.1.1（WSL `~/xuantie/bin`，target `rv64gcv_zfh_zvfh_zicbop_zihintpause`）。
vendor runtime：nncase 2.11（SDK v1.2-20260820）解压到 WSL `/tmp/nncase_rt`（含 `include_root/` + `nncase_*_runtime_linux/lib/`）。

## 标准流程

```bash
# 全量重编（WSL）
bash llm/build/build_kpu_llama.sh

# 改码后一键编译（WSL，日常用这个）
touch llm/llamacpp/ggml/src/ggml-cpu/kpu_gemm.cpp
bash llm/build/deploy_test.sh
```

产物：`build-riscv-kpu/bin/{llama-cli, llama-bench, libggml-cpu.so.0.22.0, ...}`

## 坑（都咬过人）

1. **800KB 假 .so**：cmake 时没带 `KPU_NNCASE_DIR` → 静默编出无 KPU 版本。检查 .so 体积 ~8.1MB 才对。
2. **改 CMake 后**要带 env 重新 `cmake .`，增量 make 不会重读环境变量。
3. 部署时板上三个名字都要覆盖：`libggml-cpu.so` / `.so.0` / `.so.0.22.0`，然后 `sync`。

## 路径说明

本目录脚本当前**钉死在老工作区路径**（`/mnt/d/work/git_dev/k230_prj/k230_llm`），迁移构建到本仓库布局时再参数化；先保证老流水线不断。

- `xt-toolchain.cmake` — 交叉工具链定义
- `mmz_shim.c` — 编译进 .so 的 mmz/gnne 桥（kd_mpi_sys_* + gnne_regs 预置）
- `fetch_nncase_headers3.sh` — 从 SDK 提取 vendor 头文件
- `setup/` — nncase 2.9 备用编译链安装 + PC 端 C-model 模拟器（sim_k.sh / sim29.sh）

## vendor runtime 获取（不入 git）

商业授权，不能传 GitHub。从 Canaan SDK v1.2-20260820 zip 解压：
`rt29` 备用链见 `setup/dl_rt29.sh`；2.11 现役链按交接书 §14 布局放 `/tmp/nncase_rt`。
