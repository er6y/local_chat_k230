# verify_card.ps1 - block-compare TF card (\\.\PhysicalDrive1) against debian image
# READ-ONLY on the card. No write, no format, no partition change. Result -> card_verify_result.txt
$ErrorActionPreference = 'Stop'
$img = "d:\work\git_dev\k230_prj\k230_llm\downloads\06_images\linux\CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img"
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\card_verify_v2.txt"
$len = 3467640832
$bs = 4MB

"start $(Get-Date -Format HH:mm:ss)" | Out-File $out -Encoding utf8

function Log($s) { $s | Out-File $out -Append -Encoding utf8 }

# global crash guard: anything unhandled still leaves a trace
trap {
    Log ("FATAL TRAP: " + $_.Exception.GetType().Name + ": " + $_.Exception.Message)
    Log (" at " + $_.InvocationInfo.PositionMessage)
    Log "VERIFY DONE - CRASH"
    exit 3
}

try {
    $fi = [IO.File]::OpenRead($img)
    $fd = [IO.File]::Open('\\.\PhysicalDrive1', 'Open', 'Read', 'ReadWrite')
} catch {
    Log "OPEN FAIL: $($_.Exception.Message)"
    Log "VERIFY ABORTED"
    exit 1
}
Log "both streams open ok"

$sha = [System.Security.Cryptography.SHA256]::Create()
$bufI = New-Object byte[] $bs
$bufD = New-Object byte[] $bs

function ReadExact($fs, $buf, $count) {
    $off = 0
    while ($off -lt $count) {
        $n = $fs.Read($buf, $off, $count - $off)
        if ($n -le 0) { throw "EOF/short read at offset $off" }
        $off += $n
    }
}

$pos = 0
$blocks = 0
$bad = 0
$badList = New-Object System.Collections.Generic.List[long]
$t0 = Get-Date
while ($pos -lt $len) {
    $toRead = [int]([Math]::Min([long]$bs, $len - $pos))
    try {
        ReadExact $fi $bufI $toRead
        ReadExact $fd $bufD $toRead
    } catch {
        Log "READ ERROR at card offset $pos : $($_.Exception.Message)"
        Log "VERIFY ABORTED (read error = card unreliable)"
        $fi.Close(); $fd.Close()
        Log "VERIFY DONE - READERROR"
        exit 2
    }
    $hI = $sha.ComputeHash($bufI, 0, $toRead)
    $hD = $sha.ComputeHash($bufD, 0, $toRead)
    # compare hashes byte by byte (32 bytes, cheap)
    $same = $true
    for ($i = 0; $i -lt 32; $i++) { if ($hI[$i] -ne $hD[$i]) { $same = $false; break } }
    if (-not $same) {
        $bad++
        if ($badList.Count -lt 32) { $badList.Add($pos) }
    }
    $blocks++
    $pos += $toRead
    if ($blocks % 16 -eq 0) {
        $mb = [math]::Round($pos / 1MB)
        $sec = [math]::Round(((Get-Date) - $t0).TotalSeconds, 1)
        Log "progress ${mb}MB / $sec s / bad=$bad"
    }
}
$fi.Close(); $fd.Close()
$totSec = [math]::Round(((Get-Date) - $t0).TotalSeconds, 1)
Log "compare done: blocks=$blocks badBlocks=$bad elapsed=${totSec}s"
if ($bad -gt 0) {
    Log "first mismatch offsets (bytes):"
    foreach ($o in $badList) { Log "  $o ($([math]::Round($o/1MB))MB)" }
    Log "VERDICT: CARD DATA CORRUPTED (write not retained or read unreliable)"
    Log "VERIFY DONE - MISMATCH"
} else {
    Log "VERDICT: CARD READS BACK IDENTICAL TO IMAGE (static read/write OK)"
    Log "VERIFY DONE - OK"
}
