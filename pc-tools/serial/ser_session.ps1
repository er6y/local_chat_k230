# ser_session.ps1 - serial COM4 session with auto-login, run commands, capture
# usage: ser_session.ps1 -Cmds "cmd1","cmd2" -WaitSec 12
param(
    [Parameter(Mandatory=$true)][string[]]$Cmds,
    [int]$WaitSec = 12,
    [int]$LoginTimeout = 8
)
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\ser_session_out.txt"
"=== serial session $(Get-Date -Format HH:mm:ss) ===" | Out-File $out -Encoding utf8
function Log($s) { Add-Content -Path $out -Value $s -Encoding UTF8 }

$port = New-Object System.IO.Ports.SerialPort('COM4', 115200, 'None', 8, 'One')
$port.ReadTimeout = 200
$port.DtrEnable = $true
$port.RtsEnable = $true
$port.Open()
Start-Sleep -Milliseconds 500
$null = $port.ReadExisting()

# --- probe: send newline, see if we get a prompt back
$port.Write("`r`n")
Start-Sleep -Milliseconds 800
$probe = $port.ReadExisting()
Log "--- probe: [$($probe.Trim())]"

# --- auto-login if we see a login prompt or nothing (board console may need it)
if ($probe -match 'login' -or $probe.Trim().Length -eq 0) {
    Log "--- sending login sequence (root + empty password)"
    $port.Write("root`r`n")
    Start-Sleep -Milliseconds 700
    $r1 = $port.ReadExisting()
    $port.Write("`r`n")
    Start-Sleep -Milliseconds 700
    $r2 = $port.ReadExisting()
    Log "--- login resp: [$($r1.Trim())][$($r2.Trim())]"
}
# if probe showed a shell prompt (# / $), skip login

foreach ($c in $Cmds) {
    Log (">>> " + $c)
    $port.Write($c + "`r`n")
    $deadline = (Get-Date).AddSeconds($WaitSec)
    $collected = ""
    while ((Get-Date) -lt $deadline) {
        try {
            $s = $port.ReadExisting()
            if ($s) { $collected += $s; Start-Sleep -Milliseconds 150 }
            else { Start-Sleep -Milliseconds 100 }
        } catch { Start-Sleep -Milliseconds 100 }
    }
    Log $collected
}
$port.Close()
Write-Output ("output written to " + $out)
Get-Content $out
