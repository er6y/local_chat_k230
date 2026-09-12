#!/bin/sh
# tts_abx.sh —— 三引擎 TTS 评测：同一段文本、逐句 A/B/C 连播对比
#
#   A = 官方参考设计 tts_zh（fastspeech1+2+hifigan，/mnt/data/tts_zh/run_tts.sh 同款）
#   B = aishell3 VITS（tts_daemon_start.sh 同款模型配置，一炮式跑）
#   C = piper（kpu_vits_runner --piper-dir，精度与线上 svc_tts.sh 完全一致）
#
# 用法:
#   sh /mnt/data/tts_eval/tts_abx.sh                # 用内置 7 个评测短句
#   sh /mnt/data/tts_eval/tts_abx.sh <文本文件>     # 每行一个短句，逐行跑
#   SID=10 SPEED=1.0 可覆盖 aishell 说话人/语速
#
# 评测文本已覆盖（听的时候对着挑毛病）:
#   四声+轻声(妈妈/结实/便宜/眼睛/合适) / 一·不变调(一双/一年/一样/一条/
#   不会/不紧不松) / 多音字(银行háng-自行车xíng、数着shǔ-数学shù、
#   长大zhǎng、教您jiāo、真会huì、还打hái、九五折zhé、划算huá、
#   高兴地de-笑得de) / 平翘舌(四毛-十一、数-说-顺) / 前后鼻音(紧jǐn-睛
#   jīng、缝fèng、心-兴、定) / n-l(牛奶/女/绿/旅) / ü(女绿旅语) /
#   儿化(玩儿) / 三声连读(水果/两步/百) / 数字TN(2026年9月12日、十一点半、
#   一百二十三块四毛、九五折) / 英文缩写(APP) / 疑问-感叹语调
#
# 产物: /mnt/data/tts_eval/S<n>_{A,B,C}.wav；每句三个引擎合成完立刻按
#   A→B→C 连播（间隔1s），边听边比。日志: /mnt/data/tts_eval/abx.log
#
# 注意:
#   * 建议先停 chat 栈（sh /mnt/data/chat/S99chat stop）再跑：常驻
#     llama+TTS 与一炮式合成抢 CMA/KPU，内存紧了谁都不保实时。
#   * 每句每引擎都是一炮式（重新加载模型），7 句全程约 10~15 分钟；
#     可挂后台: sh tts_abx.sh > /mnt/data/tts_eval/abx.out 2>&1 &

OUT=/mnt/data/tts_eval
LOG=$OUT/abx.log
TTSZH=/mnt/data/tts_zh
SID=${SID:-10}
SPEED=${SPEED:-1.0}

# chat.conf 给 piper 现役路径（RUNNER/TTS_DIR）与精度（TTS_*_SFX / TTS_DP_KPU）
[ -f /mnt/data/chat/chat.conf ] && . /mnt/data/chat/chat.conf
RUNNER=${RUNNER:-/mnt/data/melo/kpu_vits_runner_p}
PIPER_DIR=${TTS_DIR:-/mnt/data/melo/piper}
export PIPER_ENC_SFX=${TTS_ENC_SFX:-}
export PIPER_FLOW_SFX=${TTS_FLOW_SFX:-}
export PIPER_DEC_SFX=${TTS_DEC_SFX:-_i8}
[ "${TTS_DP_KPU:-0}" = "1" ] && export PIPER_DP_KPU=1

# aishell 引擎 runner：板上旧版优先（历史 daemon 同款），piper 版兜底
RUNNER_AISHELL=/mnt/data/aishell3/kpu_vits_runner
[ -f "$RUNNER_AISHELL" ] || RUNNER_AISHELL="$RUNNER"

for f in /mnt/data/kpu_llm/safe_run.sh "$TTSZH/tts_zh" \
         "$TTSZH/kmodel/zh_fastspeech_1.kmodel" "$RUNNER"; do
  [ -f "$f" ] || { echo "缺文件: $f" >&2; exit 1; }
done
[ -d "$PIPER_DIR" ] || { echo "缺目录: $PIPER_DIR" >&2; exit 1; }
[ -d /mnt/data/aishell3 ] || echo "警告: /mnt/data/aishell3 不在，B 引擎会失败" >&2
if pgrep -x kpu_vits_runner >/dev/null 2>&1; then
  echo "提示: 线上 TTS daemon 正在跑，建议先 svc_tts.sh stop（不影响继续跑）" >&2
fi

mkdir -p "$OUT"

SENTFILE=$(mktemp)
if [ -n "$1" ]; then
  # 文件模式: 每行一个短句；去引号（个别前端把引号处理出怪音）
  sed 's/“//g; s/”//g' "$1" > "$SENTFILE"
else
  cat > "$SENTFILE" <<'EOF'
张阿姨2026年9月12日中午十一点半去银行取钱，打算给女儿买一双绿色的旅游鞋，再买两斤水果和一提牛奶。
排队的时候，她一边数着口袋里的一百二十三块四毛，心里琢磨：
这鞋结实、漂亮、耐磨，穿一年也不会变形，现在还打九五折，挺便宜的，怪不得大家都说划算。
付完钱，她高兴地回家，把鞋递给正在复习语文和数学的小明：试试看，合不合脚？不紧不松才叫合适。
小明穿上走了两步，笑得眼睛眯成了一条缝：
妈，您可真会挑，跟定做的一样合脚！
等我长大了，就骑自行车带您去公园玩儿半天，再教您用手机APP查公交路线。
EOF
fi

# busybox date 无 %N，用 /proc/uptime 取毫秒
now_ms() { awk '{printf "%.0f", $1 * 1000}' /proc/uptime; }

echo "== tts abx $(date) sid=$SID speed=$SPEED ==" | tee -a "$LOG"

n=0
while IFS= read -r SENT; do
  [ -z "$SENT" ] && continue
  n=$((n + 1))
  echo "---- 第${n}句: $SENT" | tee -a "$LOG"

  # A 官方 tts_zh（fastspeech1+2+hifigan）
  T0=$(now_ms)
  ( cd "$TTSZH" && LD_BIND_NOW=1 KPU_LOCAL=1 TTS_OUT="$OUT/S${n}_A.wav" \
      sh /mnt/data/kpu_llm/safe_run.sh "$LOG" ./tts_zh \
      kmodel/zh_fastspeech_1.kmodel kmodel/zh_fastspeech_2.kmodel \
      kmodel/hifigan.kmodel 1 "$SENT" ) >> "$LOG" 2>&1
  T1=$(now_ms)

  # B aishell3 VITS（历史 daemon 同款模型组合）
  ( cd /mnt/data/aishell3 2>/dev/null; \
    LD_LIBRARY_PATH=/mnt/data/sherpa2/lib LD_BIND_NOW=1 KPU_LOCAL=1 \
    GNNE_QUIET=1 GNNE_POLL_SLEEP_US=50 \
    sh /mnt/data/kpu_llm/safe_run.sh "$LOG" "$RUNNER_AISHELL" \
      "$SENT" --sid="$SID" --speed="$SPEED" --output="$OUT/S${n}_B.wav" \
      --subgen-kmodel=/mnt/data/aishell3/subgen_f192.kmodel \
      --dp-kmodel=/mnt/data/aishell3/dp_only.kmodel \
      --enc-only-onnx=/mnt/data/aishell3/enc_only_sim.onnx \
      --lexicon=/mnt/data/aishell3/lexicon.txt \
      --tokens=/mnt/data/aishell3/tokens.txt \
      --rule-fsts=/mnt/data/aishell3/phone.fst,/mnt/data/aishell3/date.fst,/mnt/data/aishell3/number.fst ) >> "$LOG" 2>&1
  T2=$(now_ms)

  # C piper（精度与线上一致：a16w8 enc/flow + int8 dec）
  # 注意 runner 把 argv[1] 一律当 text、从 argv[2] 才解析选项，
  # 所以 --piper-say 前面必须垫一个占位文本
  LD_BIND_NOW=1 KPU_LOCAL=1 \
      sh /mnt/data/kpu_llm/safe_run.sh "$LOG" "$RUNNER" \
      x --piper-say="$SENT" --piper-dir="$PIPER_DIR" --speed="$SPEED" \
      --output="$OUT/S${n}_C.wav" >> "$LOG" 2>&1
  T3=$(now_ms)

  echo "    耗时ms A=$((T1 - T0)) B=$((T2 - T1)) C=$((T3 - T2))" | tee -a "$LOG"

  for v in A B C; do
    if [ -f "$OUT/S${n}_$v.wav" ]; then
      amixer cset name='PCM Switch' on >/dev/null 2>&1
      aplay "$OUT/S${n}_$v.wav" >/dev/null 2>&1
      amixer cset name='PCM Switch' off >/dev/null 2>&1
      sleep 1
    else
      echo "    S${n}_$v.wav 未生成（看 $LOG 末尾）" >&2
    fi
  done
done < "$SENTFILE"
rm -f "$SENTFILE"

echo "== 完成: $(ls "$OUT"/S*_*.wav 2>/dev/null | wc -l) 个 wav 在 $OUT ==" | tee -a "$LOG"
