# ASR（离线中文语音识别，规划）

## 方向

- 首选 C/C++ 推理栈（sherpa-onnx / funasr-onnx 类，CPU RVV），或 nncase/KPU 子图
- MNN 路线已废弃（2026-09 决策），相关脚本已归档不迁移
- 采样率/流式/首字延迟预算：对齐语音助手 <5s 端到端目标

## 依赖 LLM 一腿的结论

- KPU 混合推理栈已验证（kpu_gemm.cpp 可复用为任意 GEMM offload 骨架）
- 内存预算紧张：见 docs 交接书，ASR 引擎常驻前先算 2GB 全机预算表

## 待办

1. 选型 PoC（PC 端先行）
2. 板上 RVV 性能标定（复用 llm/eval/microbench）
3. 与 KWS 的分帧/接力接口定义
