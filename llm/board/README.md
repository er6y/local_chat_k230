# 板上运行资产

部署目标：`/mnt/data/kpu_llm/`（持久分区，reset 不丢）。

## 必部署清单

| 文件 | 作用 |
|---|---|
| `safe_run.sh` | WDT 喂狗包裹器，一切测试必须经它跑 |
| `selftest_bn2.sh` | KPU 数值自检（判据 cos>0.98） |
| `kpu_gemm_daemon.py` | socket 守护进程（KPU_DAEMON=1 备胎模式用） |
| `run_decode_test.sh` | decode 回归 |
| `oc_k230v2.py` | 板上校准工具 |
| `rootfs-overlay/S52wifi` + `usr-local-bin/wifi_keepalive.sh` | WiFi 自愈链（装到 /etc/init.d 与 /usr/local/bin） |

## 运行环境（现役基线）

```sh
cd /mnt/data/kpu_llm
export LD_LIBRARY_PATH=.
export LD_BIND_NOW=1
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen/s4sq_q8
export KPU_TILES=s4
export KPU_RESIDENT=10
export KPU_PRELOAD=0
export KPU_LOCAL=1        # 进程内 runtime（现役）；备胎用 KPU_DAEMON=1
export GNNE_QUIET=1
```

板上资产目录：`/mnt/data/kpu_qwen/`（kmodel 全集）、`/mnt/data/models/`（gguf）。

## 板上纪律（红线）

- 测试必须 `safe_run.sh` 包裹 + 落盘 `/mnt/data/*.log`
- 长任务用 `nohup setsid ... &` 后台跑，轮询 tail
- 写文件后 `sync` 再 reboot
- CmaFree < ~400MB 先 reboot（nncase load 段泄漏，长跑必脏池）
- daemon 被 kill -9 后 mmz 段滞留 → drop_caches 或重启

## gnne/ 诊断工具

`check_gnne.py` / `gnne_trace.sh` / `gnne_fast_trace.py` — GNNE 寄存器与 trace 排障用（历史上定位过 gnne_regs 未映射导致的静默零输出）。
