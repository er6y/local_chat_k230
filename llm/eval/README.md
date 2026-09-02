# 验证与测量工具

## m1/ — Path B 独立验证套件

`t_vendor.cpp` 进程内 vendor runtime 最小验证（加载 kmodel 跑一个 GEMM 对参考输出）。
这是 Path B 的 M1 门槛：cos>0.98。**历史战绩：cos=1.000000, max_abs_err=0, 0/8192 mismatch。**

```bash
bash m1/build_vendor_test.sh   # WSL 编译
bash m1/run_t_vendor.sh        # 板上跑（需 KPU_KMODEL_DIR 指向 s4sq_q8）
```

## 数值对比

- `cmp_dump.py` — 两份 .f32 dump 对比（cos/max_err/mismatch）。
  经典用法：`KPU_DUMP=l0_q` 分别跑 local 和 daemon 模式，拉回输出对比（结论 cos=1.0）。
- `cmp_out.py` / `cmp_s4.py` — 早期 kmodel 输出对比。

## 性能

- `run_perf3.sh` — prefill wall-time 测量（/proc/uptime 计时，避开 busybox date 无 %N 的坑）
- `analyze_timing3.py` — CPU GEMM 逐层耗时分析
- `pc_baseline.py` — PC 端参考基线
- `validate_mixed.sh` / `validate_fix.sh` — 混合模式回归

## fastk_verify.c + build 脚本

K230_FAST 硬编码 decode 的端到端验证器（昨晚 tg 2.58 的验证工具）。

## microbench/ — C908 硬件底探测

c908_microbench / ddr_bw / bench_vecdot / bench_vecf16 / rvv_smoke 等源码。
用于标定 RVV 向量宽度、DDR 带宽、f16 支持等硬件边界。

## notes/ — 内核工作手记

`quants_disasm.txt` / `v2_asm.txt`（反汇编参考）+ `insert_v2.py` / `rewrite_cols.py`（RVV 内核生成改写工具）。
未来做内核优化时先看这里。
