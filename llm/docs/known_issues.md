# 已知问题清单 — 错误 / 准确性 / 内存（2026-09-24 盘点）

## 一、准确性（上产前必须清）

| # | 问题 | 状态 | 缓解/修复 |
|---|---|---|---|
| A1 | **w32 滑窗质量未验收**：窗口只看最近 32 token，长依赖问答会错 | **未验收，上产前必测** | eval 集（10 长依赖题 + 20 常规题）跑 kv6_stacked vs 官方全历史对照；不行就 w64（编译参数 W 改）。**harness 方案**：chat_static.py（两模型交互驱动）改造成 kv6 五输入版，贪心生成 dump 文本离线评分——这是 A1 的第一块砖 |
| A2 | T2 peephole 数值不过：mini 逐位过，全模型 sim cos=0.9396 | 已禁用（compiler/ActionToInstruct.cs 两行注释） | 疑区边界配对变换值域假设；修复+全模型 oracle 过了再开 |
| A3 | QuantToCpu 量化噪声：q2cpu 链路输出 diff mean=0.0013（u8 单 LSB 量级） | ✅ 可接受 | 持续监控 argmax 指纹（bd_kv6q2cpu.py 应=13） |
| A4 | 单样本校准的历史遗留：早期模型复读/乱码 | ✅ 已被多样本校准+range128 根治 | 保持 calib 多样本配方 |

## 二、内存（.a 固有，只能协议化规避）

| # | 问题 | 状态 | 缓解 |
|---|---|---|---|
| M1 | **CMA 不归还**：每次大 kmodel 装载吃 ~408MB 池，进程退出也不还 | 固有 | **每次大装载之间 reboot**；产线一进程一模型常驻 |
| M2 | 每进程残留 60-70MB CMA | 固有 | 同上；python 驱动用单进程长驻（decode 循环内不复装载） |
| M3 | 早期开机 ~1.0GB 大窗口反而装载 segfault | 已定性 | 开机等 2-3 分钟再跑（S99chat 预热时序已覆盖） |
| M4 | 传大文件后页缓存挤占 CMA（CmaFree 假象 11MB） | 已定性 | 跑模型前 `sync; echo 3 > drop_caches`（bd 脚本流程含） |
| M5 | k230 root 分区 364M：模型/日志一律进 /mnt/data | 铁律 | adb push 必须 MSYS_NO_PATHCONV=1 |

## 三、已修复错误（防回归清单）

| # | 症状 | 根因 | 修复位置 |
|---|---|---|---|
| E1 | ERNIE 跑分词 273GB bad_alloc（假案"运行时元数据之谜"） | tokenizer.cpp decode 无越界检查 + replace(pos,3) 笔误 | cxx/qwen_chat_overlay/tokenizer.cpp.patch |
| E2 | 剪枝模型 terminate | 重复惩罚 scores[旧id] 越界写堆 | qwen_chat_remap（板上，.oobfix 版） |
| E3 | qwen_chat "config null" type_error 306 | config.json 传成目录 | 用法：传文件路径 |
| E4 | chatd 开机全家死绝（子进程 exec 永久阻塞） | FIFO O_WRONLY\|NONBLOCK 无读者 ENXIO | chat_service S99chat/chatd（O_RDWR 自持读者） |
| E5 | kpud 懒加载掏空 CMA → GNNE 分配失败 → 整机重启 | 池 cap 简化超发 | llm/board/kpud.cpp（恢复逐档 cap） |
| E6 | 双装载 segfault / 低 CMA 假死 | M1/M2 | 板上三铁律（board/static/README） |
| E7 | pipe 传大文件吞尾 64KB | Windows-OpenSSH→WSL 管道 | 分块+md5 规程；不走管道 |

## 四、待观察

- 官方 202ms 标尺的复现条件：扫查脚本已预置 `board/static/bench_official_matrix.py`（H×seq 矩阵，golden window 下跑）
- GNNE 大解码板上会 OOM 爆网（tools/README 有警告）；k230 模拟器路径已迁服务器
- 512 主机 WSL 已判死（lxcore.sys），机器重启被禁——512 只作只读档案
