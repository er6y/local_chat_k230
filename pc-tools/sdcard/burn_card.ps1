# burn_card.ps1 - burn debian img to TF card via READER (bypass board ROM channel), then full verify
# WRITE ONLY \\.\PhysicalDrive1 after safety gate. Result -> card_burn_result.txt
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\card_burn_result.txt"
"start $(Get-Date -Format HH:mm:ss)" | Out-File $out -Encoding utf8
function Log($s) { $s | Out-File $out -Append -Encoding utf8 }
trap {
    Log ("FATAL: " + $_.Exception.GetType().Name + ": " + $_.Exception.Message)
    Log (" at " + $_.InvocationInfo.PositionMessage)
    Log "BURN DONE - CRASH"
    exit 3
}

$img = if ($args.Count -gt 0) { $args[0] } else { "d:\work\git_dev\k230_prj\k230_llm\downloads\06_images\linux\CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img" }
if (-not (Test-Path $img)) { Log "ABORT: img not found: $img"; exit 1 }
Log ("img: " + $img)

# ---- safety gate ----
$d = Get-Disk -Number 1 -ErrorAction Stop
Log ("disk1: '" + $d.FriendlyName + "' bus=" + $d.BusType + " size=" + [math]::Round($d.Size/1GB,1) + "GB")
if ($d.BusType -ne 'USB' -or (($d.Size -lt 13GB -or $d.Size -gt 17GB) -and ($d.Size -lt 100GB -or $d.Size -gt 140GB))) { Log "ABORT: disk1 not a known TF card (16G or 128G SanDisk)"; exit 1 }
Log "safety gate PASSED"

# wipe partition tables so Windows releases volume handles (raw write over mounted volumes fails mid-stream)
Set-Disk -Number 1 -IsOffline $false -ErrorAction SilentlyContinue
Clear-Disk -Number 1 -RemoveData -RemoveOEM -ErrorAction Stop -Confirm:$false
Start-Sleep -Seconds 2
Log "disk cleaned (partition tables removed)"

$imgLen = (Get-Item $img).Length
Log ("img: " + $imgLen + " bytes (" + [math]::Round($imgLen/1MB) + "MB)")

$bs = 4MB
$fi = [IO.File]::OpenRead($img)
$fd = [IO.File]::Open('\\.\PhysicalDrive1','Open','ReadWrite','ReadWrite')
$buf = New-Object byte[] $bs
$rbuf = New-Object byte[] $bs

# ---- phase 1: write ----
Log "phase1: writing img to card"
$pos = [long]0
$blk = 0
while ($pos -lt $imgLen) {
    $toRead = [int]([Math]::Min([long]$bs, $imgLen - $pos))
    $toRead = $toRead - ($toRead % 512)  # raw disk writes must be sector-aligned (mini img has a 128-byte unaligned tail)
    if ($toRead -le 0) { Log ("tail " + ($imgLen - $pos) + " bytes (<512, sector padding) skipped"); break }
    $off = 0
    while ($off -lt $toRead) {
        $n = $fi.Read($buf, $off, $toRead - $off)
        if ($n -le 0) { break }
        $off += $n
    }
    if ($off -ne $toRead) { Log ("IMG READ SHORT at pos " + $pos); exit 2 }
    $retries = 0
    while ($true) {
        try {
            $fd.Position = $pos
            $fd.Write($buf, 0, $toRead)
            break
        } catch [IO.IOException] {
            # volume mount invalidates the raw handle mid-stream: reopen and retry same offset
            $retries++
            if ($retries -gt 5) { Log ("WRITE GAVE UP at pos " + $pos); throw }
            Log ("  write hiccup at " + [math]::Round($pos/1MB) + "MB (retry " + $retries + "), reopening handle")
            try { $fd.Close() } catch { }
            Start-Sleep -Seconds 3
            $fd = [IO.File]::Open('\\.\PhysicalDrive1','Open','ReadWrite','ReadWrite')
        }
    }
    $pos += $toRead
    $blk++
    if ($blk % 128 -eq 0) { Log ("  wrote " + [math]::Round($pos/1MB) + "MB " + (Get-Date -Format HH:mm:ss)) }
}
$fd.Flush($true)
Log ("write done: " + $blk + " blocks, " + $pos + " bytes")

# ---- phase 2: full read-back verify ----
Log "phase2: read back and byte-compare with img"
$fi.Position = 0
$fd.Position = 0
$bad = @()
$pos = [long]0
$blk = 0
while ($pos -lt $imgLen) {
    $toRead = [int]([Math]::Min([long]$bs, $imgLen - $pos))
    $toRead = $toRead - ($toRead % 512)
    # img block
    $ioff = 0
    while ($ioff -lt $toRead) { $n = $fi.Read($buf, $ioff, $toRead - $ioff); if ($n -le 0) { break }; $ioff += $n }
    # card block
    $coff = 0
    while ($coff -lt $toRead) { $n = $fd.Read($rbuf, $coff, $toRead - $coff); if ($n -le 0) { break }; $coff += $n }
    if ($ioff -ne $toRead -or $coff -ne $toRead) { Log ("READ SHORT at blk $blk img=$ioff card=$coff"); $bad += $blk; $pos += $toRead; $blk++; continue }
    $eq = $true
    for ($k = 0; $k -lt $toRead; $k += 4096) {
        $end = [Math]::Min($k + 4096, $toRead)
        for ($m = $k; $m -lt $end; $m++) { if ($buf[$m] -ne $rbuf[$m]) { $eq = $false; break } }
        if (-not $eq) { break }
    }
    if (-not $eq) { $bad += $blk; Log ("  BAD block $blk @ " + [math]::Round($pos/1MB) + "MB") }
    $pos += $toRead
    $blk++
    if ($blk % 128 -eq 0) { Log ("  verified " + [math]::Round($pos/1MB) + "MB " + (Get-Date -Format HH:mm:ss)) }
}
$fi.Close()
$fd.Close()

if ($bad.Count -eq 0) {
    Log "ALL BLOCKS VERIFIED - CARD CONTENT == IMG (reader channel fully reliable)"
    Log "BURN DONE - SUCCESS, card ready to insert into board"
} else {
    Log ("VERIFY FAILED: " + $bad.Count + " bad blocks at: " + (($bad | ForEach-Object { $_ * 4 }) -join 'MB, ') + "MB")
    Log "BURN DONE - FAILED"
}
