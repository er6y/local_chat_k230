# kmodel 资产（编译脚本 + scale 边车）

kmodel 是 nncase 编译的 KPU 子图（每层 7 个 GEMM stem：q/k/v/o/gate/up/down，共 28 层 × 7 = 196/套）。

## tile 档位

| 套 | tile | 用途 | 内存 |
|---|---|---|---|
| S16 (s4sq_q8 顶层布局) | 16x16=256 行 | prefill 大 M | ~843MB/套 |
| S4 (`s4/` 子目录) | 4x4=16 行 | decode 小 M | ~443MB/套 |
| S1 | 精确 M=1 | 实验用 | - |

现役生产集：`/mnt/data/kpu_qwen/s4sq_q8/`（Q8 源，KPU_TILES=s4）。

## compile/（从 WSL /tmp 抢救，曾两次险些随 WSL 重启蒸发）

- `mk_sq4.py` — S4 SmoothQuant 编译（nncase 2.11）
- `mk_sq4_29.py` — nncase 2.9 版（**编 2.9 kmodel 用这个**，勿用 mk_sq4.py 的 resume 逻辑）
- `mk_sq16*.py` — S16 档实验族
- `make_calib.py` — 校准数据生成（calib16/ 的 .npy 来源）
- 其余 mk_*/chk_* 为各代实验脚本，保留备查

nncase 2.9 venv：WSL `~/venv_nncase29`（PATH 需含 venv site-packages）。

## scales/（入库，~1.2MB/套）

SmoothQuant 每通道 scale 边车（`<stem>.scale`，float32 × IN）。**运行时必需**：
`.so` 加载 kmodel 时读同目录 `<stem>.scale` 做 fold 补偿。
- `scales/s4sq_q8/` — 现役 196 个（Q8 源）
- `scales/s4_sq/` — Q4 源 196 个（历史）

## .kmodel 二进制不进 git

板上 `/mnt/data/kpu_qwen/` + PC 老工作区；后续走 GitHub Release 分发 + SHA256。
