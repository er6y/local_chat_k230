# Windows 侧运维工具

## serial/ — COM4 串口（永生救援通道）

**只许 COM4 @115200**（FTDI VID_0403&PID_6001）。COM16 等是非 K230 设备，严禁打开。

| 脚本 | 用途 |
|---|---|
| `ser_session.ps1` | 交互会话 |
| `ser_reboot.ps1` | 串口发 reboot（root 登录：`root` 回车再空回车） |
| `ser_check.ps1` / `ser_read.ps1` / `ser_wake.ps1` | 探活 / 读输出 / 唤醒 console |
| `listen_com4.ps1` / `send_com4.ps1` | 监听 / 发送 |
| `connect_wifi.ps1` / `get_ip.ps1` / `inject_key.ps1` | WiFi 配置（SSID YILEIW，wpa_supplicant 加 network 块自动连） |

板子卡死判定：串口 10-15s 无任何输出（连登录符都没有）= 内核级挂死 → 物理拔电。

## sdcard/ — 烧卡与验卡

`burn_card.ps1` / `verify_card.ps1` / `repair_card.ps1` / `diag_card.ps1` / `test_sandisk.ps1`。
历史结论：山寨卡会静默写坏 → 只用正品 SanDisk；烧卡后必须 verify 全卡。
