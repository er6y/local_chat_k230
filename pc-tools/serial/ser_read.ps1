# ser_read.ps1 - read serial output for 10s
$port = New-Object System.IO.Ports.SerialPort COM4,115200,None,8,one
$port.ReadTimeout = 10000
$port.WriteTimeout = 3000
try {
    $port.Open()
    Start-Sleep -Milliseconds 300
    $port.Write("`r`n")
    $buf = ""
    $end = (Get-Date).AddSeconds(10)
    while ((Get-Date) -lt $end) {
        try {
            $ch = $port.ReadChar()
            $buf += [char]$ch
        } catch {
            Start-Sleep -Milliseconds 100
        }
    }
    if ($buf.Length -eq 0) { Write-Host "(no output)" }
    else { Write-Host $buf }
} catch {
    Write-Host "ERROR: $_"
} finally {
    if ($port.IsOpen) { $port.Close() }
}
