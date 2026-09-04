#!/bin/sh
# run_tts.sh <text> — one-shot Chinese TTS on big-core Linux.
# Output: /mnt/data/tts_zh/tts_out.wav (16-bit PCM mono 24 kHz)
# Requires: LD_BIND_NOW=1 (KPU precond), watchdog-guarded via safe_run.sh
# KPU_LOCAL=1 is MANDATORY: the gnne cstage staging path segfaults on these
# old-format (LDMK v7) kmodels; raw pass-through enable works.
cd /mnt/data/tts_zh || exit 1
LOG=/mnt/data/tts_zh/tts.log
export LD_BIND_NOW=1
export KPU_LOCAL=1
export TTS_OUT=/mnt/data/tts_zh/tts_out.wav
sh /mnt/data/kpu_llm/safe_run.sh "$LOG" ./tts_zh \
  kmodel/zh_fastspeech_1.kmodel kmodel/zh_fastspeech_2.kmodel kmodel/hifigan.kmodel 1 "$1"
