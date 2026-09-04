#!/bin/sh
# tts_daemon_start.sh — launch the resident TTS worker (models load ONCE).
# Requests come through /tmp/tts_cmd (one line per utterance):
#   <wav_path>\t<sid>\t<speed>\t<text>
# tail -f keeps the pipe open so per-writer EOF never reaches the worker.
# Worker runs under nice 19 (LLM decode preempts it) and the watchdog.
cd /mnt/data/aishell3 || exit 1
LOG=/mnt/data/aishell3/tts_daemon.log
FIFO=/tmp/tts_cmd

# idempotent start
if [ -n "$(pgrep -f 'kpu_vits_runner --daemon')" ]; then
  echo "tts daemon already running"
  exit 0
fi
[ -p "$FIFO" ] || mkfifo "$FIFO"

export LD_LIBRARY_PATH=/mnt/data/sherpa2/lib
export LD_BIND_NOW=1
export KPU_LOCAL=1
export GNNE_QUIET=1
export GNNE_POLL_SLEEP_US=50

nohup setsid sh -c "
  nice -n 19 sh /mnt/data/kpu_llm/safe_run.sh '$LOG' sh -c '
    tail -f $FIFO | /mnt/data/aishell3/kpu_vits_runner --daemon \
      --subgen-kmodel=/mnt/data/aishell3/subgen_f192.kmodel \
      --dp-kmodel=/mnt/data/aishell3/dp_only.kmodel \
      --enc-only-onnx=/mnt/data/aishell3/enc_only_sim.onnx \
      --lexicon=/mnt/data/aishell3/lexicon.txt \
      --tokens=/mnt/data/aishell3/tokens.txt \
      --rule-fsts=/mnt/data/aishell3/phone.fst,/mnt/data/aishell3/date.fst,/mnt/data/aishell3/number.fst'
" > /dev/null 2>&1 < /dev/null &

# wait for READY (up to 40s: ort load ~7s + lexicon + kpu init)
for i in $(seq 1 80); do
  grep -q '^READY' "$LOG" 2>/dev/null && break
  sleep 0.5
done
grep -q '^READY' "$LOG" && echo "TTS_DAEMON_READY" || echo "TTS_DAEMON_NOT_READY (check $LOG)"
