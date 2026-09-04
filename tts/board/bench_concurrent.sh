#!/bin/sh
# bench_concurrent.sh — the heaviest case: LLM decode + TTS running at once.
# Phase 1: llama decode alone (baseline tg rate)
# Phase 2: llama decode in background + tts_zh in foreground
# Logs everything to /mnt/data/tts_zh/bench.log (persistent).
# The KPU daemon must be running for llama --kpu prefill; decode itself is CPU.
cd /mnt/data || exit 1
LOG=/mnt/data/tts_zh/bench.log
M=/mnt/data/models/qwen3-q4km.gguf
TXT='今天天气不错，我们一起去公园散步，然后回家吃饭'

export LD_LIBRARY_PATH=/mnt/data/kpu_llm
export KPU_KMODEL_DIR=/mnt/data/kpu_qwen
export KPU_RESIDENT=200
export KPU_TILES=s4
export KPU_PRELOAD=1

echo "== bench_concurrent $(date) ==" > $LOG

# ---- phase 1: decode baseline (no TTS) ----
echo "[phase1] llama decode baseline" >> $LOG
sh /mnt/data/kpu_llm/safe_run.sh $LOG /mnt/data/kpu_llm/llama-bench \
    -m $M -t 1 -p 128 -n 96

# ---- phase 2: concurrent ----
echo "[phase2] llama decode + TTS concurrently" >> $LOG
sh /mnt/data/kpu_llm/safe_run.sh /mnt/data/tts_zh/llama_conc.log \
    /mnt/data/kpu_llm/llama-bench -m $M -t 1 -p 128 -n 96 &
LLAMA_PID=$!

T0=$(date +%s%3N)
LD_BIND_NOW=1 TTS_OUT=/mnt/data/tts_zh/tts_out.wav \
    sh /mnt/data/kpu_llm/safe_run.sh $LOG /mnt/data/tts_zh/tts_zh \
    /mnt/data/tts_zh/kmodel/zh_fastspeech_1.kmodel \
    /mnt/data/tts_zh/kmodel/zh_fastspeech_2.kmodel \
    /mnt/data/tts_zh/kmodel/hifigan.kmodel 1 "$TXT" >> $LOG 2>&1
TTS_RC=$?
T1=$(date +%s%3N)
TTS_MS=$((T1 - T0))

wait $LLAMA_PID
grep -E "tg|pp" /mnt/data/tts_zh/llama_conc.log >> $LOG

WAV_LEN=$(stat -c %s /mnt/data/tts_zh/tts_out.wav 2>/dev/null || echo 0)
AUDIO_SEC=$(( (WAV_LEN - 44) / 48000 ))   # PCM16 mono 24kHz = 48 bytes/ms
echo "[phase2] TTS wall ${TTS_MS} ms (rc=$TTS_RC), audio ~${AUDIO_SEC} s, realtime=$(awk "BEGIN{printf \"%.2f\", $AUDIO_SEC*1000/($TTS_MS+1)}")x" >> $LOG
echo "== done ==" >> $LOG
cat $LOG
