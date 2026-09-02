# inject_key.ps1 - add our public key to board sshd via COM4
$pub = (Get-Content "$env:USERPROFILE\.ssh\id_ed25519.pub" -First 1).Trim()
$cmd = "mkdir -p /root/.ssh ; echo '$pub' >> /root/.ssh/authorized_keys ; chmod 700 /root/.ssh ; chmod 600 /root/.ssh/authorized_keys ; wc -l /root/.ssh/authorized_keys"
$p = New-Object System.IO.Ports.SerialPort('COM4',115200,'None',8,'One')
$p.ReadTimeout = 5000
$p.Open()
$p.Write("`n")
Start-Sleep 1
$null = $p.ReadExisting()
$p.Write($cmd + "`n")
Start-Sleep 4
$out = $p.ReadExisting()
$p.Close()
$out
