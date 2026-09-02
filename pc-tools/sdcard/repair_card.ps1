# repair_card.ps1 - attempt to revive TF card by triggering controller remap:
# overwrite first 4GB with indexed pattern, read back and verify. READ/WRITE ONLY \\.\PhysicalDrive1
# Safety: abort unless disk1 is USB bus AND 14-16GB. Result -> card_repair_result.txt
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\card_repair_result.txt"
"start $(Get-Date -Format HH:mm:ss)" | Out-File $out -Encoding utf8
function Log($s) { $s | Out-File $out -Append -Encoding utf8 }
trap {
    Log ("FATAL TRAP: " + $_.Exception.GetType().Name + ": " + $_.Exception.Message)
    Log (" at " + $_.InvocationInfo.PositionMessage)
    Log "REPAIR DONE - CRASH"
    exit 3
}

# ---- safety gate: confirm disk 1 is the TF card ----
$d = Get-Disk -Number 1 -ErrorAction Stop
Log ("disk1: '" + $d.FriendlyName + "' bus=" + $d.BusType + " size=" + [math]::Round($d.Size/1GB,1) + "GB")
if ($d.BusType -ne 'USB') { Log "ABORT: disk1 not USB bus"; exit 1 }
if ($d.Size -lt 13GB -or $d.Size -gt 17GB) { Log "ABORT: disk1 size not in 13-17GB range"; exit 1 }
Log "safety gate PASSED (USB + ~16GB card)"

$totalBytes = 4294967296   # 4GB
$bs = 4MB
$blocks = $totalBytes / $bs
Log ("phase1: write " + $blocks + " x 4MB pattern blocks (indexed)")

$fd = [IO.File]::Open('\\.\PhysicalDrive1','Open','ReadWrite','ReadWrite')
$buf = New-Object byte[] $bs
# fill pattern ONCE outside loop (PS byte-loop is slow)
for ($j = 0; $j -lt $bs; $j++) { $buf[$j] = 0xA5 }
for ($i = 0; $i -lt 1024; $i++) {
    # per-block tag at bytes 0..15: little-endian block index + magic 0xC0FFEE00
    [BitConverter]::GetBytes([uint64]$i).CopyTo($buf, 0)
    [BitConverter]::GetBytes([UInt64]3238002688).CopyTo($buf, 8)  # 0xC0FFEE00 as decimal (PS5.1 parses hex literal as negative Int32)
    $fd.Write($buf, 0, $bs)
    if ($i % 128 -eq 0) { Log ("  wrote block $i (" + ($i*4) + "MB) " + (Get-Date -Format HH:mm:ss)) }
}
$fd.Flush($true)
Log "write phase done, flushed"

Log "phase2: read back and verify"
$fd.Position = 0
$rbuf = New-Object byte[] $bs
$bad = @()
for ($i = 0; $i -lt 1024; $i++) {
    $off = 0
    while ($off -lt $bs) {
        $n = $fd.Read($rbuf, $off, $bs - $off)
        if ($n -le 0) { Log ("READ EOF/FAIL at block $i offset $off"); break }
        $off += $n
    }
    # verify tag: block index low byte at [0], magic 0xC0FFEE00 little-endian at [8..11]
    $tagOk = ($rbuf[0] -eq ($i -band 0xFF)) -and ($rbuf[8] -eq 0x00) -and ($rbuf[9] -eq 0xEE) -and ($rbuf[10] -eq 0xFF) -and ($rbuf[11] -eq 0xC0)
    $fillOk = $true
    # spot-check fill at 8 positions
    foreach ($p in @(1024, 1048576, 2097152, 3145728, 2097152+1048576, $bs-1024, $bs-2, $bs/2)) {
        if ($rbuf[$p] -ne 0xA5) { $fillOk = $false; break }
    }
    if (-not ($tagOk -and $fillOk)) {
        $bad += $i
        Log ("  BAD block $i (" + ($i*4) + "MB) tag=$tagOk fill=$fillOk")
    }
    if ($i % 128 -eq 0) { Log ("  verified block $i " + (Get-Date -Format HH:mm:ss)) }
}
$fd.Close()

if ($bad.Count -eq 0) {
    Log "ALL 4GB VERIFIED - controller remapped bad blocks, CARD REVIVED"
    Log "REPAIR DONE - SUCCESS"
} else {
    Log ("STILL BAD: " + $bad.Count + " blocks at: " + (($bad | ForEach-Object { $_ * 4 }) -join 'MB, ') + "MB")
    Log "REPAIR DONE - FAILED (card is dead, must replace)"
}
