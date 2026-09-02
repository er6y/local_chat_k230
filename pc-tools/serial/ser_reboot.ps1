# ser_reboot.ps1 - send reboot via COM4 serial
$port = New-Object System.IO.Ports.SerialPort COM4,115200,None,8,one
$port.ReadTimeout = 3000
$port.WriteTimeout = 3000
try {
    $port.Open()
    Start-Sleep -Milliseconds 500
    # send Ctrl-C to break any running output
    $port.Write([char]3)
    Start-Sleep -Milliseconds 300
    $port.Write("`r`n")
    Start-Sleep -Milliseconds 500
    $port.Write("root`r`n")
    Start-Sleep -Milliseconds 1000
    $port.Write("`r`n")
    Start-Sleep -Milliseconds 1000
    $port.Write("reboot`r`n")
    Start-Sleep -Milliseconds 500
    Write-Host "reboot sent"
    try {
        while ($true) {
            $line = $port.ReadLine()
            Write-Host $line
        }
    } catch {}
} catch {
    Write-Host "ERROR: $_"
} finally {
    if ($port.IsOpen) { $port.Close() }
}
