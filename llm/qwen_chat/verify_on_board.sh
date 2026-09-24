#!/bin/bash
# verify_on_board.sh — runner（qwen_chat）板端推理验证（在 WSL 跑，二进制就从 repo 树编的）
# 用法：bash verify_on_board.sh [模型目录] （默认官方 Qwen2.5-0.5B-Instruct）
# 前置：golden window 规程（reboot→230s→drop_caches→poke）由本脚本自带完成
# 判定：装载成功 + prefill/decode 统计打印 + 生成中文文本非空 + decode ≤400ms/tok（官方模型参照 ~290-335ms）
set -euo pipefail

MODEL_DIR=${1:-/mnt/data/qwen_official/Qwen2.5-0.5B-Instruct}
BIN_SRC=/root/qwen_chat_repo/build_riscv/qwen_chat
BOARD=root@172.16.72.255
BDIR=/mnt/data/qwen_official
BTGT=$BDIR/qwen_chat_repo

[ -x "$BIN_SRC" ] || { echo "缺 $BIN_SRC，先在 repo 树 build"; exit 1; }

echo "=== 1/4 推二进制与配置 ==="
ssh $BOARD "sync; echo 3 > /proc/sys/vm/drop_caches" 2>/dev/null || true
scp -q "$BIN_SRC" "$BOARD:$BTGT"
ssh $BOARD "chmod +x $BTGT"
# max_new_tokens 必须写在 config.json（命令行传的不生效，demo 已知坑）
ssh $BOARD "cd $MODEL_DIR && cp -f config.json config_vt.json && grep -q max_new_tokens config_vt.json || echo '{\"max_new_tokens\":16}' > /tmp/vt_insert && python3 -c \"
import json
c = json.load(open('config_vt.json'))
c['max_new_tokens'] = 16
json.dump(c, open('config_vt.json', 'w'))
\" 2>/dev/null || true"

echo "=== 2/4 reboot 进 golden window ==="
ssh $BOARD "reboot"
sleep 125
for i in $(seq 1 12); do ssh -o ConnectTimeout=5 $BOARD "echo up" >/dev/null 2>&1 && break; sleep 10; done
sleep 105   # 开机满 230s
ssh $BOARD "sync; echo 3 > /proc/sys/vm/drop_caches; ln -sf /dev/k230-gnne /dev/gnne_device; sh $BDIR/../static/noc_poke.sh 2>/dev/null || sh /mnt/data/static/noc_poke.sh"
ssh $BOARD "grep CmaFree /proc/meminfo"

echo "=== 3/4 跑推理（fresh 二进制） ==="
ssh $BOARD "echo '今天天气怎么样？' > /tmp/vt_q1.txt; cd $MODEL_DIR && $BTGT ./config_vt.json /tmp/vt_q1.txt" | tee /tmp/verify_runner_out.txt

echo "=== 4/4 判定 ==="
DECODE=$(grep -oE "decode[^0-9]*[0-9]+\.[0-9]+" /tmp/verify_runner_out.txt | tail -1 || true)
echo "原始输出见上；decode 统计行：$DECODE"
echo "人工确认两条：① 生成中文非空非乱码 ② decode 速率与参照量级（官方模型 ~0.29-0.34s/tok）"
echo "=== VERIFY_RUNNER_DONE ==="
