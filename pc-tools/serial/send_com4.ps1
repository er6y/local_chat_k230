# send_com4.ps1 - send commands to K230 serial console and capture output
# usage: send_com4.ps1 -Cmds "cmd1","cmd2" -WaitSec 15
param(
    [Parameter(Mandatory=$true)][string[]]$Cmds,
    [int]$WaitSec = 15
)
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\serial_cmd_out.txt"
"=== cmd session $(Get-Date -Format HH:mm:ss) ===" | Out-File $out -Encoding utf8
function Log($s) { Add-Content -Path $out -Value $s -Encoding UTF8 }

$port = New-Object System.IO.Ports.SerialPort('COM4', 115200, 'None', 8, 'One')
$port.ReadTimeout = 300
$port.DtrEnable = $true
$port.RtsEnable = $true
$port.Open()
Start-Sleep -Milliseconds 500
# drain pending input
$null = $port.ReadExisting()

foreach ($c in $Cmds) {
    Log (">>> " + $c)
    $port.Write($c + "`r`n")
    $deadline = (Get-Date).AddSeconds($WaitSec)
    $collected = ""
    while ((Get-Date) -lt $deadline) {
        try {
            $s = $port.ReadExisting()
            if ($s) { $collected += $s; Start-Sleep -Milliseconds 200 }
            else { Start-Sleep -Milliseconds 100 }
        } catch { Start-Sleep -Milliseconds 100 }
    }
    Log $collected
}
$port.Close()
Write-Output ("output written to " + $out)
Get-Content $out
