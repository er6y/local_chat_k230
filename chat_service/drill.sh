#!/bin/sh
# drill.sh — 防呆故障注入测试(2026-09-14):每项注入后验证自动恢复
# 2026-09-15b: D3 改60s轮询(chatd 退避 1s->2s->4s... 随连杀升级,4s 窗口误报)。
# 2026-09-15: D2 v3——chatd 重启 TTS 时会 O_TRUNC 重建 tts_out.log,DONE 绝对计数会归零,
#            跨截断比较必然误报。注入成功以 kvr pid 变化为准;合成以截断后当前日志
#            出现 ≥2 条 DONE 为准(截断先于注入后首句,日志内 DONE 必为注入后产出)。
R=/tmp/chat
echo "== D0 配置漂移校验 =="
md5sum /mnt/data/chat/chat.conf /mnt/data/kpu_llm/safe_run.sh /mnt/data/chat/ask.sh | cut -c1-8,40-
grep -E "^KPU_ENABLE|^LLMD_LOAD_MODE|^KPU_CAP16|^KPU_LOCAL_FLOOR" /mnt/data/chat/chat.conf
echo "== D1 杀llmd(回答途中)→ask.sh v2应自动重发并拿到答案 =="
sh /mnt/data/chat/ask.sh "请数到十，一个一个数" > $R/_d1.log 2>&1 &
AP=$!
sleep 8
kill -9 $(pgrep -x llmd) 2>/dev/null && echo "  injected: llmd killed"
wait $AP
grep -q "十" $R/_d1.log && echo "  D1 PASS: 重发后拿到答案" || echo "  D1 FAIL: 无答案: $(tail -1 $R/_d1.log)"
echo "== D2 杀TTS(合成途中)→chatd拉起,句子排队补合成 =="
OLDK=$(pgrep -x kvr_new | head -1)
sh /mnt/data/chat/ask.sh "你好呀" > $R/_d2.log 2>&1 &
AP=$!
sleep 3
kill -9 $OLDK 2>/dev/null && echo "  injected: kvr($OLDK) killed"
wait $AP
sh /mnt/data/chat/ask.sh "再说一遍你好" > $R/_d2b.log 2>&1
# 轮询:最长240s,等截断后的日志攒够2条DONE(kvr冷加载慢是已知代价)
T=0; N=0
while [ $T -lt 240 ]; do
  N=$(grep -ac "^DONE" $R/tts_out.log 2>/dev/null); [ -z "$N" ] && N=0
  [ "$N" -ge 2 ] && break
  sleep 10; T=$((T+10))
done
NEWK=$(pgrep -x kvr_new | head -1)
if [ "$NEWK" != "$OLDK" ] && [ "$N" -ge 2 ]; then
  W=$(ls /tmp/chat/utt_* 2>/dev/null | wc -l)
  echo "  D2 PASS: 语音恢复(kvr $OLDK->$NEWK, DONE=$N, 未播积压=$W, 增量耗时${T}s)"
else
  echo "  D2 FAIL: pid变更=$([ "$NEWK" != "$OLDK" ] && echo yes || echo no) DONE=$N"
fi
echo "== D3 杀player(播放中)→chatd应拉起(退避随连杀升级,最长等60s) =="
kill -9 $(pgrep -f "player.s[h]") 2>/dev/null && echo "  injected: player killed"
T=0
while [ $T -lt 60 ]; do
  pgrep -f "player.s[h]" >/dev/null && break
  sleep 2; T=$((T+2))
done
pgrep -f "player.s[h]" >/dev/null && echo "  D3 PASS: player 已自愈(耗时${T}s)" || echo "  D3 FAIL: 60s未恢复"
echo "== DRILLS_END =="
