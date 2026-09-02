# ser_check.ps1 - check board via COM4 serial
$port = New-Object System.IO.Ports.SerialPort COM4,115200,None,8,one
$port.ReadTimeout = 5000
$port.WriteTimeout = 3000
try {
    $port.Open()
    Start-Sleep -Milliseconds 500
    $port.Write([char]3)
    Start-Sleep -Milliseconds 500
    $port.Write("`r`n")
    Start-Sleep -Milliseconds 1000
    $port.Write("root`r`n")
    Start-Sleep -Milliseconds 1500
    $port.Write("`r`n")
    Start-Sleep -Milliseconds 1000
    $port.Write("cat /proc/meminfo | grep -i cma`r`n")
    Start-Sleep -Milliseconds 2000
    $port.Write("ip addr show wlan0 | grep inet`r`n")
    Start-Sleep -Milliseconds 2000
    $buf = ""
    try {
        while ($true) {
            $ch = $port.ReadChar()
            $buf += [char]$ch
            if ($buf.Length -gt 4000) { break }
        }
    } catch {}
    Write-Host $buf
} catch {
    Write-Host "ERROR: $_"
} finally {
    if ($port.IsOpen) { $port.Close() }
}
