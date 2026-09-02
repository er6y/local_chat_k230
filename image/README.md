# Linux 镜像定制

## 发行版决策：留 miniLinux/CanMV（buildroot），不上 Debian

| 维度 | miniLinux | Debian |
|---|---|---|
| 启动 | ~5-10s | 25-45s |
| 用户态内存 | ~50-80MB | 300-500MB（2GB 里 CMA 已占 1GB，差价致命） |
| 包生态 | SDK menuconfig 交叉编译 | apt 全，但 riscv64 轮子薄；推理栈全 C/C++ 不需要 |
| KPU 驱动 | 厂商主线，现役验证 | 同内核但用户态组合未验证 |

Debian 实验记录见 `notes/`（check*_debian.sh / watch_debian.ps1 / gunzip_debian.ps1），结论是不采用。

## 分区规划（SD 16-32GB）

```
p1 boot   ~128MB  ro   spl/uboot/kernel/dtb/小核固件（预留 A/B）
p2 rootfs ~3GB    ro   llama-cli、.so、nncase rt、ASR/TTS 引擎、S90assistant
p3 models ~3GB    ro   gguf 604MB + kmodel 443MB + 未来 ASR/TTS 模型
p4 data   其余    rw   用户文件/对话记录/设置，首开机自动扩容
```

设计要点：
- **rootfs 只读挂载**（断电安全，ext4 journal 回滚已咬过两次）
- 系统升级=刷 p2，模型升级=刷 p3，互不干扰；用户数据隔离 p4
- 开机链：rcS → 挂 p2/p3 ro + p4 rw → assistant 守护进程（WDT 喂狗，沿用 safe_run 模式）→ IPC 通知小核亮屏

## dtb/

`new_lcd4.dtb` + `fix_dtb*.sh` — LCD 屏点亮实验（未来屏幕 UI 的 DTB 基础）。

## notes/

历史镜像实验全记录：分区表手术（fix_parted_finish / fix_s00）、rootfs 注入（inject_*）、
miniLinux 探测（probe_minilinux*）、Debian 验证。做镜像定制前先读。
