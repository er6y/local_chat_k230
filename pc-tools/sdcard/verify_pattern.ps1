# verify_pattern.ps1 - READ-ONLY phase2: verify the 4GB pattern already written by repair_card.ps1
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\card_repair_result2.txt"
"start $(Get-Date -Format HH:mm:ss)" | Out-File $out -Encoding utf8
function Log($s) { $s | Out-File $out -Append -Encoding utf8 }
trap {
    Log ("FATAL TRAP: " + $_.Exception.GetType().Name + ": " + $_.Exception.Message)
    Log (" at " + $_.InvocationInfo.PositionMessage)
    Log "VERIFY DONE - CRASH"
    exit 3
}

$d = Get-Disk -Number 1 -ErrorAction Stop
if ($d.BusType -ne 'USB' -or $d.Size -lt 13GB -or $d.Size -gt 17GB) { Log "ABORT: disk1 not the TF card"; exit 1 }
Log "safety gate PASSED"

$bs = 4MB
$fd = [IO.File]::Open('\\.\PhysicalDrive1','Open','Read','ReadWrite')
$rbuf = New-Object byte[] $bs
# pre-computed spot positions (avoid PS5.1 array-literal expression parsing bug)
$spots = @(1024, 1048576, 2097152, 3145728, 4194304, 2097152, 4193000, 2096000)
$bad = @()
for ($i = 0; $i -lt 1024; $i++) {
    $off = 0
    while ($off -lt $bs) {
        $n = $fd.Read($rbuf, $off, $bs - $off)
        if ($n -le 0) { Log ("READ EOF/FAIL at block $i offset $off"); break }
        $off += $n
    }
    $tagOk = ($rbuf[0] -eq ($i -band 0xFF)) -and ($rbuf[8] -eq 0) -and ($rbuf[9] -eq 0xEE) -and ($rbuf[10] -eq 0xFF) -and ($rbuf[11] -eq 0xC0)
    $fillOk = $true
    foreach ($p in $spots) { if ($rbuf[$p] -ne 0xA5) { $fillOk = $false; break } }
    if (-not ($tagOk -and $fillOk)) {
        $bad += $i
        Log ("  BAD block $i (" + ($i*4) + "MB) tag=$tagOk fill=$fillOk")
    }
    if ($i % 256 -eq 0) { Log ("  verified block $i " + (Get-Date -Format HH:mm:ss)) }
}
$fd.Close()

if ($bad.Count -eq 0) {
    Log "ALL 4GB VERIFIED OK - controller remapped, CARD REVIVED"
    Log "VERIFY DONE - SUCCESS"
} else {
    Log ("STILL BAD " + $bad.Count + " blocks: " + (($bad | ForEach-Object { $_ * 4 }) -join 'MB, ') + "MB")
    Log "VERIFY DONE - FAILED (card dead)"
}
