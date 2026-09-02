#!/bin/sh
# wifi_keepalive.sh - auto-reconnect wlan0 whenever wpa_state drops
# (survives CMA pressure stalls, driver hiccups, AP reboots)
while true; do
    sleep 20
    st=$(wpa_cli -i wlan0 status 2>/dev/null | grep wpa_state= | cut -d= -f2)
    ip=$(ifconfig wlan0 2>/dev/null | grep 'inet addr' | wc -l)
    if [ "$st" != "COMPLETED" ] || [ "$ip" = "0" ]; then
        logger -t wifi_keepalive "reconnecting (state=$st ip=$ip)"
        killall wpa_supplicant 2>/dev/null
        rm -f /var/run/wpa_supplicant/wlan0
        ifconfig wlan0 down 2>/dev/null
        sleep 1
        ifconfig wlan0 up
        wpa_supplicant -B -i wlan0 -c /etc/wpa_supplicant.conf
        sleep 6
        udhcpc -i wlan0 -b -q -t 5 -T 3
    fi
done
