# get_ip.ps1 - dhcp on wlan0 over COM4
$cmd = @'
udhcpc -i wlan0 -b -q -t 10 -T 3 2>&1 | tail -3
ifconfig wlan0 | grep inet
'@
$p = New-Object System.IO.Ports.SerialPort('COM4',115200,'None',8,'One')
$p.ReadTimeout = 25000
$p.Open()
$p.Write("`n")
Start-Sleep 1
$null = $p.ReadExisting()
$p.Write($cmd + "`n")
Start-Sleep 25
$out = $p.ReadExisting()
$p.Close()
$out
