#!/bin/sh
# noc_poke.sh — KPU NOC QoS 解锁（官方 qwen_chat run.sh 同款）
# 来源：/mnt/data/qwen_official/run.sh，注释 "kpu noc infinite"
# 实测收益（2026-09-24，kv6_stacked 24L decode）：375→268ms（-28%）；
# 官方模型同条件 403→290ms。重启即失效，开机必须重打。
# 逐条作用（按寄存器域推断）：
#   0x91103028 = 0x30002  KPU/NOC QoS 优先级提升（主开关）
#   0x91301d0c = 0        解除限流
#   0x91301d8c = 0        解除限流
# 幂等：重复写无害。devmem 需 root。

devmem 0x91103028 32 0x30002
devmem 0x91301d0c 32 0
devmem 0x91301d8c 32 0

# 现值回显（0x91213400 期望 0x700，仅读）
echo "noc_poke done: 0x91213400=$(devmem 0x91213400 2>/dev/null)"
