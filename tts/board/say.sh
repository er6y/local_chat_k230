#!/bin/sh
# say.sh "text" — LLM 输出 → 逐句 TTS 的串接胶水（v1，正确性优先）。
# 按 。！？； 断句，每句一个 tts_zh 进程（重载 kmodel，慢但稳），
# WAV 落 /mnt/data/tts_zh/say_N.wav，总耗时打 /mnt/data/tts_zh/say.log。
# v2 再优化：单进程常驻 + 流式（首句 <1s 延迟目标）。
LOG=/mnt/data/tts_zh/say.log
echo "== say $(date) text=[$1]" >> $LOG
i=0
echo "$1" | awk -v RS='[。！？；]' 'NF { gsub(/^[[:space:]]+|[[:space:]]+$/, ""); if (length($0) > 0) print }' | while read -r line; do
    [ -z "$line" ] && continue
    i=$((i + 1))
    T0=$(date +%s%3N)
    LD_BIND_NOW=1 KPU_LOCAL=1 TTS_OUT=/mnt/data/tts_zh/say_$i.wav \
        sh /mnt/data/kpu_llm/safe_run.sh $LOG /mnt/data/tts_zh/tts_zh \
        /mnt/data/tts_zh/kmodel/zh_fastspeech_1.kmodel \
        /mnt/data/tts_zh/kmodel/zh_fastspeech_2.kmodel \
        /mnt/data/tts_zh/kmodel/hifigan.kmodel 1 "$line" >> $LOG 2>&1
    RC=$?
    T1=$(date +%s%3N)
    echo "[say] sent $i: [$line] rc=$RC wall=$((T1 - T0)) ms -> say_$i.wav" >> $LOG
done
echo "[say] done, $(ls /mnt/data/tts_zh/say_*.wav 2>/dev/null | wc -l) sentences" >> $LOG
