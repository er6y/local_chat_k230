# deploy_tts.ps1 — push tts_zh binary + kmodels + dict data to the K230 board
# PowerShell 5.1. Uses adb (VID_1209 device). Files >2MB are split into 2MB
# chunks (adb "oversize data message" bug) and reassembled on the board.
# Usage: powershell -ExecutionPolicy Bypass -File deploy_tts.ps1
$ErrorActionPreference = 'Stop'

$Repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$Bin  = Join-Path $Repo 'tts\build-tts\tts_zh'
$Kdir = 'D:\work\git_dev\k230_prj\downloads\kmodel_tts\ai_poc\kmodel'
$Fdir = 'D:\work\git_dev\k230_prj\downloads\kmodel_tts\ai_poc\utils\file'
$Board = '/mnt/data/tts_zh'

function Invoke-Adb([string]$Args) {
    adb shell $Args
    if ($LASTEXITCODE -ne 0) { throw "adb shell failed: $Args" }
}

# Push one file, chunked if larger than 2MB (adb oversize bug workaround).
function Push-File([string]$Local, [string]$Remote) {
    $len = (Get-Item $Local).Length
    if ($len -le 1900000) {
        adb push $Local $Remote | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "adb push failed: $Local" }
        Write-Host "push ok  $Remote  ($len bytes)"
        return
    }
    # chunk: 2000000 bytes each
    $tmp = Join-Path $env:TEMP 'tts_chunks'
    if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
    New-Item -ItemType Directory -Path $tmp | Out-Null
    $bytes = [IO.File]::ReadAllBytes($Local)
    $i = 0; $off = 0
    while ($off -lt $bytes.Length) {
        $n = [Math]::Min(2000000, $bytes.Length - $off)
        $chunk = New-Object byte[] $n
        [Array]::Copy($bytes, $off, $chunk, 0, $n)
        [IO.File]::WriteAllBytes((Join-Path $tmp ("c{0:D2}" -f $i)), $chunk)
        $off += $n; $i++
    }
    $pieces = @()
    for ($k = 0; $k -lt $i; $k++) {
        $p = "$Remote.p$k"
        adb push (Join-Path $tmp ("c{0:D2}" -f $k)) $p | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "adb push chunk $k failed: $Local" }
        $pieces += $p
    }
    $catlist = $pieces -join ' '
    adb shell "cat $catlist > $Remote" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "adb reassemble failed: $Remote" }
    adb shell "rm -f $catlist" | Out-Null
    Write-Host "push ok  $Remote  ($len bytes, $i chunks)"
}

Write-Host '== deploy tts_zh =='
adb devices | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'adb not available' }

adb shell "mkdir -p $Board/kmodel $Board/file/pinyin_txt"
if ($LASTEXITCODE -ne 0) { throw 'mkdir failed' }

Push-File $Bin "$Board/tts_zh"
Push-File (Join-Path $Kdir 'zh_fastspeech_1.kmodel') "$Board/kmodel/zh_fastspeech_1.kmodel"
Push-File (Join-Path $Kdir 'zh_fastspeech_2.kmodel') "$Board/kmodel/zh_fastspeech_2.kmodel"
Push-File (Join-Path $Kdir 'hifigan.kmodel') "$Board/kmodel/hifigan.kmodel"
Push-File (Join-Path $Fdir 'pinyin_txt\pinyin.txt') "$Board/file/pinyin_txt/pinyin.txt"
Push-File (Join-Path $Fdir 'pinyin_txt\small_pinyin.txt') "$Board/file/pinyin_txt/small_pinyin.txt"
Push-File (Join-Path $Fdir 'phone_id_map_en.txt') "$Board/file/phone_id_map_en.txt"

# run script: force LF line endings (PS 5.1 would otherwise keep CRLF)
$run = Get-Content (Join-Path $PSScriptRoot 'run_tts.sh') -Raw
$run = $run -replace "`r`n", "`n"
$tmpRun = Join-Path $env:TEMP 'run_tts.sh'
[IO.File]::WriteAllText($tmpRun, $run)
Push-File $tmpRun "$Board/run_tts.sh"
adb shell "chmod +x $Board/tts_zh $Board/run_tts.sh" | Out-Null

# concurrent bench script (same LF treatment)
$bench = Get-Content (Join-Path $PSScriptRoot 'bench_concurrent.sh') -Raw
$bench = $bench -replace "`r`n", "`n"
$tmpBench = Join-Path $env:TEMP 'bench_concurrent.sh'
[IO.File]::WriteAllText($tmpBench, $bench)
Push-File $tmpBench "$Board/bench_concurrent.sh"

# LLM -> TTS glue script (same LF treatment)
$say = Get-Content (Join-Path $PSScriptRoot 'say.sh') -Raw
$say = $say -replace "`r`n", "`n"
$tmpSay = Join-Path $env:TEMP 'say.sh'
[IO.File]::WriteAllText($tmpSay, $say)
Push-File $tmpSay "$Board/say.sh"

# reuse the proven watchdog wrapper from the llama deployment
adb shell "cp /mnt/data/kpu_llm/safe_run.sh $Board/safe_run.sh 2>/dev/null || echo 'WARN: safe_run.sh not copied'"

adb shell "sync"
Write-Host '== deploy done (synced) =='
Write-Host 'test:  adb shell "sh /mnt/data/tts_zh/run_tts.sh 你好，我是小智"'
