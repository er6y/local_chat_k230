# watch_debian.ps1 - wait for K230-Debian USB device (VID_1D6B) after flash, then probe shell
# RED LINE: only touch VID_1D6B devices, never other COM ports
param(
    [int]$TimeoutSec = 600
)
$log = "d:\work\git_dev\k230_prj\k230_llm\.tools\debian_watch_log.txt"
"" | Out-File $log -Encoding utf8
function Wlog($s) { ("[" + (Get-Date).ToString("HH:mm:ss") + "] " + $s) | Out-File $log -Append -Encoding utf8 }
Wlog "watch start, waiting VID_1D6B up to $TimeoutSec s"

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$found = $false
while ((Get-Date) -lt $deadline) {
    $devs = Get-CimInstance Win32_PnPEntity | Where-Object { $_.PNPDeviceID -match 'VID_1D6B' }
    if ($devs) {
        foreach ($d in $devs) {
            Wlog ("FOUND: " + $d.Name + " | " + $d.PNPDeviceID + " | err=" + $d.ConfigManagerErrorCode)
        }
        $cdc = $devs | Where-Object { $_.Name -match 'COM\d+' } | Select-Object -First 1
        if ($cdc -and $cdc.Name -match '\(COM\d+\)') {
            $port = $Matches[0].Trim('(',')')
            Wlog ("CDC port: " + $port + " - opening with DTR+RTS")
            $p = New-Object System.IO.Ports.SerialPort($port, 115200)
            $p.Encoding = [System.Text.Encoding]::GetEncoding(28591)
            $p.ReadTimeout = 300; $p.WriteTimeout = 4000
            $p.DtrEnable = $true; $p.RtsEnable = $true
            try {
                $p.Open()
                $got = ""
                $deadline2 = (Get-Date).AddSeconds(60)
                while ((Get-Date) -lt $deadline2) {
                    if ($p.BytesToRead -gt 0) { $got += $p.ReadExisting() }
                    $p.Write("`r`n")
                    if ($got -match 'root@|\\\$ |# ') { break }
                    Start-Sleep -Milliseconds 1000
                }
                if ($got.Length -gt 0) {
                    Wlog ("SHELL RESP: " + ($got -replace "`r","" -replace "`n","|"))
                    $p.Write("uname -a; cat /root/gadget.log 2>/dev/null; systemctl is-active usb-gadget`r`n")
                    Start-Sleep -Milliseconds 4000
                    $r = ""
                    $idle = 0
                    while ($idle -lt 15) {
                        if ($p.BytesToRead -gt 0) { $r += $p.ReadExisting(); $idle = 0 } else { Start-Sleep -Milliseconds 200; $idle++ }
                    }
                    Wlog ("DIAG RESP: " + ($r -replace "`r",""))
                } else { Wlog "CDC silent (no shell response in 60s)" }
                $p.Close()
            } catch { Wlog ("open fail: " + $_.Exception.Message) }
            $found = $true
            break
        }
    }
    if ($found) { break }
    Start-Sleep -Seconds 2
}
if (-not $found) { Wlog "TIMEOUT - no VID_1D6B device appeared. Debian boot likely failed." }
Wlog "watch end"
