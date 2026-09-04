# tts_zh 大核 Linux 移植（官方 demo → /mnt/data/tts_zh）

官方 SDK ai_demo 的 tts_zh（fastspeech1 + fastspeech2 + hifigan，G2P 前端）移植到
大核 Linux。目标是先单独跑通、听音质；音质过关再和 llama 并发串成语音助手闭环。

## 目录

| 路径 | 内容 |
|---|---|
| `vendor/buildroot-overlay/package/ai_demo/` | 官方源码原样 checkout（tts_zh/、common/、kws/） |
| `port/` | 大核适配层（只增不改 vendor 树） |
| `board/` | 部署/测试脚本（deploy_tts.ps1、test_tts.ps1、run_tts.sh、bench_concurrent.sh） |
| `build-tts/tts_zh` | 交叉编译产物（gitignore，不提交） |

## port/ 适配点（vs vendor 的差异）

1. **mmz.h / port_shim.c** — 官方 `mmz.h` 来自 mpp sysroot，大核没有。只声明
   main.cc 实际调用的 `kd_mpi_mmz_init/deinit`，实现为空操作。nncase k230 runtime
   真正依赖的 `kd_mpi_sys_mmz_*` 由 llama 项目验证过的 `llm/build/mmz_shim.c`
   提供（/dev/mmz ioctl + CMA），与 llama daemon 同款。
2. **k230_init.cc** — gnne_regs preseed（/dev/mem @0x80400000）+ gnne_init，从
   kpu_gemm.cpp 的验证序列复制。必须在任何 kmodel 加载之前调用（main 里最先跑）。
3. **ai_base.h/.cc** — 适配 nncase 2.11 API：
   - 去掉 `#include "utils.h"`（拉 opencv，本文件用不到）；
   - get_output() 重写：2.11 里 `to_host()` 返回全新 host tensor，随对象析构而
     释放，所以把 host tensor 和 map 视图保存在成员里，保证 p_outputs_ 指针在
     post_process 读取期间有效（v2 老 API 的 impl 链已不存在）。
4. **play_pcm.cc** — ALSA 播放替换为 WAV 落盘（同签名 `playerPcm`，vendor 的
   play_pcm.h 里的 RIFF/FMT/DATA 结构体直接用）。输出路径 `$TTS_OUT`，默认
   ./tts_out.wav。
5. **main.cc** — 合成管线原样保留；差异：先 k230_kpu_init()；stdin 循环改为
   直到 EOF（原版固定 3 轮）；可选 argv[5] 直接合成该句（LLM 串接路径，一句一进程）。

## 编译（WSL）

```bash
bash /mnt/d/work/git_dev/k230_prj/local_chat_k230/tts/port/build_tts.sh
```

要求 /tmp/nncase_rt（2.11 runtime tgz + include_root 全量头文件）仍在。产物
`build-tts/tts_zh`：静态 libstdc++/libgcc，动态 glibc，约 7.6MB，与板上
llama .so 的 KPU 约束一致（LD_BIND_NOW=1 由 run 脚本设置）。

## 已排除的风险 / 板上实测结果

- ✅ **旧格式 kmodel 兼容性**：官方 kmodel 是 LDMK 魔数 + v7（nncase 1.x/2.0
  时代格式）。已用 2.11 代码库的 x86 Simulator 实测 load_model 通过（同 reader
  代码），板上实测三个 kmodel 全部加载成功。
- ✅ **KPU 运行**：**必须 KPU_LOCAL=1**（gnne_enable 原样透传）。默认的
  cstage staging 路径在第一次推理时 segfault（老 kmodel 传给 gnne_enable 的
  pc 范围异常，staging memcpy 读飞）；透传模式下三个模型全部正常执行。
  run_tts.sh/say.sh 已内置该环境变量，勿丢。
- ✅ **退出干净**：mmz 段在 atexit 释放会与 runtime 静态析构顺序冲突导致
  退出期 segfault（llama 历史同族问题）；main 现在 mmz_shim_release_all()
  + _exit(0)，rc=0，CMA 无泄漏。
- ⚠️ **实时率**：4.89s 音频耗时 13.0s（0.375x；短句 0.21x，固定开销摊薄后
  长句更高）。fs1 921ms / fs2 1200ms / hifigan 2154ms。够用但未达流式，
  优化方向待测（每 invoke 的 gnne 同步开销、子向量分批）。
- ⚠️ **音质**：峰值 90% 满幅无削波、能量连续、韵律起伏正常——数据像语音，
  但"情绪价值"要人耳判。听 `tts/output/tts_out_long.wav`。

## 板上使用

```sh
# Windows 侧部署（adb，>2MB 自动分块）：
powershell -ExecutionPolicy Bypass -File tts/board/deploy_tts.ps1

# 板上一句合成（WDT 包裹，日志 /mnt/data/tts_zh/tts.log）：
adb shell "sh /mnt/data/tts_zh/run_tts.sh '今天天气不错，我们去公园散步吧'"

# 拉回 WAV 听效果：
powershell -ExecutionPolicy Bypass -File tts/board/test_tts.ps1

# 最重 case：llama decode + TTS 并发（判定串接后 decode 掉不掉）：
adb shell "nohup setsid sh /mnt/data/tts_zh/bench_concurrent.sh > /mnt/data/tts_zh/launcher.log 2>&1 < /dev/null &"
```

## 下一手（板回来后按序）

1. deploy + 单句合成，拉 WAV 听音质（判据：自然度是否够"情绪价值"）。
2. 实时率：合成 5 秒音频花多久（>1x 实时才有流式串接资格）。
3. bench_concurrent.sh：llama decode 掉速多少 + TTS 实时率。
4. 音质/性能不过关 → 评估 Kokoro（80MB，CPU 预算按第 3 步基线算）。
5. 串接设计：LLM 输出 → 断句（。！？）→ 逐句 tts_zh 进程 → WAV 顺序播放。
