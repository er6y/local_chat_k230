# push_dtb.ps1 - push new_lcd4.dtb to K230 over serial (base64 chunks), verify md5, install into boot partition
$ErrorActionPreference = 'Stop'
$file = "d:\work\git_dev\k230_prj\k230_llm\.tools\new_lcd4.dtb"
$remote = "/media/k230-canmv-01studio-lcd.dtb"
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\push_dtb_out.txt"
"=== push session $(Get-Date -Format HH:mm:ss) ===" | Out-File $out -Encoding utf8
function Log($s) { Add-Content -Path $out -Value $s -Encoding UTF8 }

$b64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
$md5 = (Get-FileHash $file -Algorithm MD5).Hash.ToLower()
Log ("file: $file bytes: $((Get-Item $file).Length) md5: $md5 b64len: $($b64.Length)")

$port = New-Object System.IO.Ports.SerialPort('COM4', 115200, 'None', 8, 'One')
$port.ReadTimeout = 300
$port.DtrEnable = $true
$port.RtsEnable = $true
$port.Open()
Start-Sleep -Milliseconds 500
$null = $port.ReadExisting()

function Send($s) { $port.Write($s + "`r`n") }

# clean slate
Send "rm -f /tmp/push.b64 /tmp/new.dtb"
Start-Sleep -Milliseconds 300
$null = $port.ReadExisting()

# chunked upload (drain echo every 20 chunks to keep rx buffer small)
$chunkLen = 128
$chunks = [Math]::Ceiling($b64.Length / $chunkLen)
$t0 = Get-Date
for ($i = 0; $i -lt $chunks; $i++) {
    $len = [Math]::Min($chunkLen, $b64.Length - $i*$chunkLen)
    $piece = $b64.Substring($i*$chunkLen, $len)
    $port.Write("echo -n $piece >> /tmp/push.b64`r`n")
    Start-Sleep -Milliseconds 80
    if (($i % 20) -eq 19) { $null = $port.ReadExisting() }
}
$null = $port.ReadExisting()
Log ("upload done: $chunks chunks in $(((Get-Date)-$t0).TotalSeconds.ToString('F1'))s")

# decode + verify
Send "base64 -d /tmp/push.b64 > /tmp/new.dtb; md5sum /tmp/new.dtb; wc -c /tmp/new.dtb"
Start-Sleep -Seconds 3
$r = $port.ReadExisting()
Log "--- verify output ---"
Log $r
if ($r -notmatch $md5) { Log "MD5 MISMATCH - NOT INSTALLING"; $port.Close(); Get-Content $out; exit 1 }

# install: mount p1, backup old dtb, replace, sync, verify, umount
Send "mount -t ext4 /dev/mmcblk0p1 /media && cp /media/k230-canmv-01studio-lcd.dtb /tmp/old_lcd.dtb.bak && cp /tmp/new.dtb $remote && sync && md5sum $remote && umount /media && echo INSTALLED_OK"
$deadline = (Get-Date).AddSeconds(20)
$col = ""
while ((Get-Date) -lt $deadline) {
    $s = $port.ReadExisting()
    if ($s) { $col += $s; Start-Sleep -Milliseconds 200 } else { Start-Sleep -Milliseconds 100 }
}
Log "--- install output ---"
Log $col
$port.Close()
if ($col -match 'INSTALLED_OK') { Log "PUSH DONE - dtb installed, ready to reboot" } else { Log "INSTALL OUTPUT UNCLEAR - check log" }
Get-Content $out
