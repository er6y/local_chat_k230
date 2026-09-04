# aishell3 VITS KPU 加速工具链（2026-09 攻坚成果）

完整过程见 `docs/00_交接书_2026-09-04_TTS-KPU攻坚_终版.md`（六章节，含全部根因分析）。
WSL 路径约定：ONNX 源在 `/tmp/vits-icefall-zh-aishell3/`，编译产物在 `/tmp/kpu_poc/`，
校准语料在 `/tmp/kpu_poc/aishell3_calib/`。

## 最终生产形态（板上 /mnt/data/aishell3/）

```
text → word_wrap.txt 多音字包裹（#$|词|$#）
     → lexicon.txt（含补的音乐/乐器/乐曲/乐队词条）
     → enc_only_sim.onnx   CPU f32 动态（mu/logs/dp_x 三输出，~195ms/句）
     → dp_only_min.kmodel  KPU 混合量化（值敏感算子 f32 + conv i8，39-46ms/句）
     → subgen_f192.kmodel  KPU int8
RTF 0.168-0.23, CPU ~11%。run via tts_daemon_start.sh + tts_say.sh。
```

## 脚本索引（按流水线顺序）

| 脚本 | 作用 |
|---|---|
| surgery_flow.py | NonZero→GatherND→ScatterND 全画布改写（bit-exact），flows.{3,5,7} |
| prune_compile_dp.py | 死节点剪枝 + 去废弃输入 |
| exp_scatter_static.py / exp_rewire_sweep.py | ScatterND 写回无副作用证明（14 组逐位一致） |
| verify_canvas.py | canvas vs 原图 A/B 对拍 |
| mk_dp_only.py | enc/dp 真边界切图（after_norm 转置 = dp_x 入口），enc 全排除 |
| mk_dpx_calib.py | 20 真实句 dp_x 校准集 + 零填充语义验证（0 帧漂移） |
| mk_dp_only_k230.py | dp_only int8 编译（参数主序校准！见交接书 PTQ 布局坑） |
| mk_dp_only_scheme.py | 导出量化 scheme（QuantScheme.json） |
| mk_dp_only_mixed.py | 全 mixed 编译（flows 非 conv 全 f32，精度最好较慢） |
| mk_dp_min.py | **最小集混合（现役）**：值敏感算子 f32，布局算子保 i8 |
| mk_enc_only_dyn.py | enc 动态切图（三输出） |
| mk_enc_sim.py | enc onnxsim（动态形状模式） |
| mk_probe_bins.py / cmp_probes.py | 板上 kmodel 探针对拍工具（vs 动态金标） |

## 关键教训（详见交接书）

1. nncase PTQ 校准必须**参数主序**传参（C# 侧 sample-major 分块 + python zip 转置）
2. 2.11 DumpTraceInfoPass 崩 Erf → dump_ir/dump_asm 必须关
3. 板上 riscv ORT 对 NonZero/ScatterND 动态图求值有错——**不要拿它当对拍参照**
4. int8 伤 transformer enc（mu 漂移 2.3 → 鸟语）；dp（时长）对 int8 容忍但尾部敏感 → 混合量化
5. K230 KPU 是 8bit MAC：int16 精度完美但慢 8 倍，f32 无加速
6. sherpa Chinese 逐字查词典，多字词条需 `#$|..|$#` 触发（word_wrap.txt 机制）
7. scp 大文件崩板子 WiFi → `scp -l 400` 限速
