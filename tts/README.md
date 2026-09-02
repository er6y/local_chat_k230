# TTS（离线中文语音合成，规划）

## 方向

- 声码器优先 CPU RVV（f16 向量已验证可用，见 llm/eval/microbench 的 bench_vecf16）
- 候选：轻量 acoustic model + RVV 量化 vocoder；或 nncase/KPU 子图（复用 kpu_gemm 骨架）
- 目标：首句音频 <1s 延迟（流水线化：首 token 即可起播）

## 待办

1. 选型 PoC + 板上延迟标定
2. 与 LLM decode 的流式接力（token → 音素 → 音频 chunk）
3. 音频输出通道验证（板子声卡/I2S 状态未验证——语音助手最大未知项之一）
