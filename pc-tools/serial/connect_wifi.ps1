# connect_wifi.ps1 - join YILEIW on wlan0 over COM4 console
$cmd = @'
printf 'ctrl_interface=/var/run/wpa_supplicant\nnetwork={\nssid="YILEIW"\npsk="18615701937"\n}\n' > /tmp/wpa.conf
killall wpa_supplicant 2>/dev/null
ifconfig wlan0 up
wpa_supplicant -B -i wlan0 -c /tmp/wpa.conf
sleep 4
wpa_cli -i wlan0 status 2>/dev/null | grep -E 'wpa_state|ssid|ip_address'
'@
$p = New-Object System.IO.Ports.SerialPort('COM4',115200,'None',8,'One')
$p.ReadTimeout = 10000
$p.Open()
$p.Write("`n")
Start-Sleep 1
$null = $p.ReadExisting()
$p.Write($cmd + "`n")
Start-Sleep 10
$out = $p.ReadExisting()
$p.Close()
$out
