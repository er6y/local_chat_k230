# board/ — 板端资产与生产基础设施

部署目标：`/mnt/data/`（持久分区）。**两个时代并存，别搞混：**

## 现役 = static/（静态 kmodel 测量与打分）

qwen25 kv6_stacked 线的板端权威工具全在 **`static/`**（详见其 README）：
`bd_kv6q2cpu.py`（计时+指纹判定）、`noc_poke.sh`（NOC QoS 解锁）、
`bench_official_matrix.py`（官方 202ms 条件扫）、剖析/取证脚本族。
一键流水线 `../oneclick/qwen25.sh` 的 bench 段调的就是这里。

## 生产基础设施（顶层，chatd/kpud 时代）

| 文件 | 作用 | 状态 |
|---|---|---|
| `chatd.cpp` / `kpud.cpp` | 语音助手守护进程源码（chatd 总管 + kpud KPU 池） | **生产栈 2026-09-18 停用**（用户裁定"等隔壁把 llm 弄好再说"），恢复 = `chmod +x /etc/init.d/S99chat` |
| `safe_run.sh` | WDT 喂狗包裹器，一切长任务必须经它跑 | ✅ 现役纪律 |
| `selftest_bn2.sh` | KPU 数值自检（判据 cos>0.98） | ✅ 现役 |
| `rootfs-overlay/S52wifi` + `usr-local-bin/wifi_keepalive.sh` | WiFi 自愈链（装 /etc/init.d 与 /usr/local/bin） | ✅ 现役（8189fs 5.11.6 配套） |
| `gnne/` | GNNE 寄存器/trace 排障（定位过 gnne_regs 未映射静默零输出） | 排障时用 |

## lab/（一次性调试脚本归档）

kpud 守护进程时代的排障脚本：kpu_leak/probe/repro/real_test、selftest_bindnow、
start_daemon_test、chat_sim、samp_ab、run_decode_test、oc_k230v2（板上校准）、
kpu_gemm_daemon（KPU_DAEMON 备胎模式）。**只有复现历史问题才进来**；
背景见 docs/00~03 号交接书（2026-09-01 ~ 09-13）。

## 板上纪律（红线，长存）

- 测试必须 `safe_run.sh` 包裹 + 落盘 `/mnt/data/*.log`
- 长任务 `nohup setsid ... &`，轮询 tail；写文件后 `sync` 再 reboot
- **CMA 三铁律**：大装载之间必 reboot；开机满 230s 才跑；跑模型前 drop_caches
  （详见 `../oneclick/qwen25.sh` bench 段的内置实现）
- 模型/日志只进 /mnt/data（root 分区 364M 红线）
