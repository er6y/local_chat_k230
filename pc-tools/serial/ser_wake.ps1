# ser_wake.ps1 - wake serial console and read
$port = New-Object System.IO.Ports.SerialPort COM4,115200,None,8,one
$port.ReadTimeout = 15000
$port.WriteTimeout = 3000
try {
    $port.Open()
    Start-Sleep -Milliseconds 500
    # send several Ctrl-C + Enter to break any state
    for ($i=0; $i -lt 3; $i++) {
        $port.Write([char]3)
        Start-Sleep -Milliseconds 200
        $port.Write("`r`n")
        Start-Sleep -Milliseconds 500
    }
    $buf = ""
    $end = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $end) {
        try {
            $ch = $port.ReadChar()
            $buf += [char]$ch
        } catch {
            Start-Sleep -Milliseconds 100
        }
    }
    if ($buf.Length -eq 0) { Write-Host "(no output - board may be hung)" }
    else { Write-Host $buf }
} catch {
    Write-Host "ERROR: $_"
} finally {
    if ($port.IsOpen) { $port.Close() }
}
