# 小核固件 + 大小核 IPC（规划）

## 职责划分（K230 双核架构）

| 核 | 职责 |
|---|---|
| 小核 C906 @RT-Thread | **实时 IO**：MIPI 摄像头、LCD 屏、LED、按键（01Studio CanMV 固件已暴露 MicroPython API） |
| 大核 C908 @Linux | **推理**：KWS/ASR/LLM/TTS + WiFi |

屏幕显示助手状态 = 小核职责，与大核解耦；大核只发状态消息。

## IPC 协议草案（先定协议再写代码）

- 通道：共享内存 + 简单消息队列（k230 IPC），或 virtuart 退路
- 状态机：`idle / listening / thinking / speaking / error`
- 载荷：状态枚举 + 可选 UTF-8 文本（屏显回复摘要/音量/网络状态）
- 方向：大核 → 小核 为主；小核 → 大核 保留按键事件位

## 待办

1. 定协议格式（对齐/长度/校验）
2. 小核侧 CanMV MicroPython 消费端 demo
3. 大核侧 C 发送端（llama-cli 生成回调处挂钩）
