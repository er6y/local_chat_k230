# listen_com4.ps1 - serial console listener for K230 debug UART (FTDI COM4)
# logs to serial_log.txt and echoes to stdout
$ErrorActionPreference = 'Stop'
$log = "d:\work\git_dev\k230_prj\k230_llm\.tools\serial_log.txt"
$port = $null
try {
    $port = New-Object System.IO.Ports.SerialPort('COM4', 115200, 'None', 8, 'One')
    $port.ReadTimeout = 200
    $port.DtrEnable = $true
    $port.RtsEnable = $true
    $port.Open()
    Write-Output "=== COM4 OPEN 115200, logging to $log ==="
    while ($true) {
        try {
            $s = $port.ReadExisting()
            if ($s.Length -gt 0) {
                Write-Output -NoNewline $s
                [IO.File]::AppendAllText($log, $s)
            } else {
                Start-Sleep -Milliseconds 50
            }
        } catch [TimeoutException] { }
    }
} catch {
    Write-Output ("SERIAL ERROR: " + $_.Exception.Message)
} finally {
    if ($port -and $port.IsOpen) { $port.Close() }
    Write-Output "=== COM4 CLOSED ==="
}
