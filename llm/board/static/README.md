# board/static/ — 静态 kmodel 板上脚本（/mnt/data/static/）

开机自启产线由 `chat_service/S99chat` 管；本目录是**研发测量/取证脚本**，
使用前先过一遍"板上三铁律"：

1. **NOC poke 必打**：`sh noc_poke.sh`（-28%，见文件头注释；S99chat 已内置）
2. **CMA 黄金窗口**：reboot → 等 ~230s → `ln -sf /dev/k230-gnne /dev/gnne_device`
   → `sync; echo 3 > /proc/sys/vm/drop_caches` → CmaFree ≥560MB。
   **每次大装载之间必须 reboot**（.a 装载的 CMA 池不归还，每进程还漏 60-70MB）
3. **大文件传输**：分块 + 逐块 md5（`compile/push_stacked.sh` 现成）

## 计时/剖析

- `bd_kv6q2cpu.py <kmodel> [reps]` — **权威计时**（干净随机数据，5 输入，argmax 指纹应=13）
- `bd_prof_kv6q2cpu.py` — set_profiling 时间线（**剖析态数字偏大 ~2×，只看相对结构**；
  timeline 会 dump 多块，块[2] 起为稳态）
- `bd_prof_off.py` — 官方模型剖析（mask 必须 [1,1,1,1]，喂错烧 CMA）
- `bd_hold.py` — 装载后挂住进程，供 /proc/pid/maps、pagemap、mmz 取证

## 取证/解剖

- `id3/id4/id5/id6/id7.py` — kmodel 函数表 × 时间线 join（tsz 指纹、快慢区对比、字节 diff）
- `id11.py`（pagemap 物理地址采样）、`id12.py`（/dev/mmz 映射清单=物理布局）
- `fncount.py <kmodel>` — 区数/tsz 分布（官方 vs 我们对比的第一入口）
- `cfgcmp.py` / `addrmap.py` — 快慢区指令构成对比
- `mmz_hold.py <MB>` — mmz 前置占位（分配布局扰动实验）

## 环境

- `noc_poke.sh` — KPU NOC QoS 解锁三条 devmem（来源官方 run.sh）
- `probe_clk.sh` — 时钟/PLL 状态扫描
- `ab_run.sh` — 带 poke 的标准跑批（含环境变量）
- `decode_kvwin.py` — 旧 kvwin 滑窗解码驱动（96 输出版；堆叠版消费端
  改读 output1/2 = ktnew_all/vtnew_all [48,1,1,64]，索引 l*2+g）
