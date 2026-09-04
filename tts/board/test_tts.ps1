# test_tts.ps1 — run one TTS utterance on the board and pull the WAV back.
# Usage: powershell -ExecutionPolicy Bypass -File test_tts.ps1 [-NoDeploy] [-Text "..."]
param(
    [switch]$NoDeploy,
    [string]$Text = '今天天气不错，我们去公园散步吧'
)
$ErrorActionPreference = 'Stop'
$Out = Join-Path $PSScriptRoot 'output'
if (!(Test-Path $Out)) { New-Item -ItemType Directory -Path $Out | Out-Null }

if (!$NoDeploy) {
    & (Join-Path $PSScriptRoot 'deploy_tts.ps1')
}

$cmd = "sh /mnt/data/tts_zh/run_tts.sh '$Text'"
Write-Host "== run: $cmd"
adb shell $cmd
if ($LASTEXITCODE -ne 0) { throw 'launch failed' }

# poll the log until safe_run finishes (rc line) or timeout
$deadline = (Get-Date).AddMinutes(5)
do {
    Start-Sleep -Seconds 5
    $tail = (adb shell "tail -5 /mnt/data/tts_zh/tts.log" 2>$null) -join "`n"
    Write-Host "---- log tail ----"
    Write-Host $tail
    if ($tail -match 'done rc=') { break }
} while ((Get-Date) -lt $deadline)

adb pull /mnt/data/tts_zh/tts_out.wav (Join-Path $Out 'tts_out.wav')
Write-Host "== wav pulled to $Out\tts_out.wav — play it and judge the quality =="
