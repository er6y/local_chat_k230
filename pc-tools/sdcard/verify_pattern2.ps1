# verify_pattern2.ps1 - READ-ONLY full-byte verify of the 4GB pattern (fixed magic + no OOB spots)
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\card_repair_result3.txt"
"start $(Get-Date -Format HH:mm:ss)" | Out-File $out -Encoding utf8
function Log($s) { $s | Out-File $out -Append -Encoding utf8 }
trap {
    Log ("FATAL: " + $_.Exception.GetType().Name + ": " + $_.Exception.Message)
    Log (" at " + $_.InvocationInfo.PositionMessage)
    Log "VERIFY2 DONE - CRASH"
    exit 3
}

$d = Get-Disk -Number 1 -ErrorAction Stop
if ($d.BusType -ne 'USB' -or $d.Size -lt 13GB -or $d.Size -gt 17GB) { Log "ABORT: not the TF card"; exit 1 }
Log "safety gate PASSED"

$bs = 4MB
$fd = [IO.File]::Open('\\.\PhysicalDrive1','Open','Read','ReadWrite')
$rbuf = New-Object byte[] $bs
$exp = New-Object byte[] $bs
for ($j = 0; $j -lt $bs; $j++) { $exp[$j] = 0xA5 }

$bad = @()
for ($i = 0; $i -lt 1024; $i++) {
    # rebuild expected tag: same expressions the writer used
    [BitConverter]::GetBytes([uint64]$i).CopyTo($exp, 0)
    [BitConverter]::GetBytes([UInt64]3238002688).CopyTo($exp, 8)

    $off = 0
    while ($off -lt $bs) {
        $n = $fd.Read($rbuf, $off, $bs - $off)
        if ($n -le 0) { Log ("READ EOF at block $i"); break }
        $off += $n
    }
    if (-not [System.Linq.Enumerable]::SequenceEqual($rbuf, $exp)) {
        # find first diff offset for diagnostics
        $dif = -1
        for ($k = 0; $k -lt $bs; $k += 512) {
            $segEqual = $true
            for ($m = $k; $m -lt ($k + 512); $m++) { if ($rbuf[$m] -ne $exp[$m]) { $segEqual = $false; break } }
            if (-not $segEqual) { $dif = $k; break }
        }
        $bad += $i
        Log ("  BAD block $i (" + ($i*4) + "MB) first-diff@+" + $dif)
    }
    if ($i % 256 -eq 0) { Log ("  verified block $i " + (Get-Date -Format HH:mm:ss)) }
}
$fd.Close()

if ($bad.Count -eq 0) {
    Log "ALL 1024 BLOCKS FULL-BYTE VERIFIED - PATTERN INTACT"
    Log "VERIFY2 DONE - SUCCESS (card holds data correctly)"
} else {
    Log ("BAD " + $bad.Count + "/1024 blocks: " + (($bad | ForEach-Object { $_ * 4 }) -join 'MB, ') + "MB")
    Log "VERIFY2 DONE - FAILED"
}
