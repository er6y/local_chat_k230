# test_sandisk.ps1 - spot write/read test on new SanDisk 128G card
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\sandisk_test.txt"
"start $(Get-Date -Format HH:mm:ss)" | Out-File $out -Encoding utf8
function Log($s) { $s | Out-File $out -Append -Encoding utf8 }

# safety: disk1 must be USB and 100-140GB (the SanDisk 128G)
$d = Get-Disk -Number 1 -ErrorAction Stop
if ($d.BusType -ne 'USB' -or $d.Size -lt 100GB -or $d.Size -gt 140GB) { Log "ABORT: not the SanDisk 128G"; exit 1 }
Log ("disk1: " + $d.FriendlyName + " " + [math]::Round($d.Size/1GB,1) + "GB - safety gate PASSED")

# wipe partition table first so raw IO is clean
Clear-Disk -Number 1 -RemoveData -RemoveOEM -Confirm:$false -ErrorAction Stop
Start-Sleep -Seconds 2
Log "disk cleaned"

$fd = [IO.File]::Open('\\.\PhysicalDrive1','Open','ReadWrite','ReadWrite')
$bs = 1MB
$buf = New-Object byte[] $bs
$rbuf = New-Object byte[] $bs

# spot offsets incl. the exact sectors where the old card failed + powers-of-2 boundaries + far end
$spots = @(0, 64MB, 127MB, 128MB, 129MB, 256MB, 296MB, 511MB, 512MB, 513MB, 1GB, 2GB, 8GB, 32GB, 64GB, 100GB, 118GB)
$bad = 0
foreach ($off in $spots) {
    # fill with offset-derived pattern
    $tag = [BitConverter]::GetBytes([UInt64]($off / 1MB))
    for ($i = 0; $i -lt $bs; $i += 512) { $tag.CopyTo($buf, $i) }
    try {
        $fd.Position = [long]$off
        $fd.Write($buf, 0, $bs)
        $fd.Flush($true)
        $fd.Position = [long]$off
        $n = 0; while ($n -lt $bs) { $r = $fd.Read($rbuf, $n, $bs - $n); if ($r -le 0) { break }; $n += $r }
        $ok = [System.Linq.Enumerable]::SequenceEqual($buf, $rbuf)
        if ($ok) { Log ("offset {0,6} MB: OK" -f ($off/1MB)) } else { Log ("offset {0,6} MISMATCH" -f ($off/1MB)); $bad++ }
    } catch {
        $ex = $_.Exception; while ($ex.InnerException) { $ex = $ex.InnerException }
        Log ("offset {0,6} MB: FAIL {1}" -f ($off/1MB), $ex.Message); $bad++
    }
}
$fd.Close()
if ($bad -eq 0) { Log "SPOT TEST PASS - card writable and readable"; Log "TEST DONE - SUCCESS" } else { Log ("TEST DONE - FAILED (" + $bad + " bad spots)") }
