#!/bin/sh
# tts_say.sh <wav_path> <sid> <speed> <text> — one utterance through the
# resident TTS daemon; blocks until the WAV exists (or 30s timeout).
# Returns 0 on success; prints DONE line from the worker log tail.
WAV="$1"; SID="${2:-10}"; SPD="${3:-1.0}"; TEXT="$4"
if [ -z "$TEXT" ]; then
  echo "usage: tts_say.sh <wav_path> <sid> <speed> <text>" >&2
  exit 2
fi
[ -p /tmp/tts_cmd ] || { echo "tts daemon not started (no /tmp/tts_cmd)" >&2; exit 3; }

rm -f "$WAV"
printf '%s\t%s\t%s\t%s\n' "$WAV" "$SID" "$SPD" "$TEXT" > /tmp/tts_cmd

i=0
while [ ! -f "$WAV" ] && [ $i -lt 300 ]; do
  sleep 0.1
  i=$((i + 1))
done
if [ -f "$WAV" ]; then
  echo "TTS_DONE $WAV"
  exit 0
fi
echo "TTS_TIMEOUT $WAV" >&2
exit 1
