# sd_verify.ps1 - login and verify new 4-bit/50MHz SD config + dd speed test
$cmds = @(
    'root',
    'dmesg | grep -iE mmc0 | head -12',
    'dmesg | grep -ci i/o',
    'S=$(cut -d. -f1 /proc/uptime); dd if=/dev/mmcblk0p3 of=/dev/null bs=1M count=64 2>/dev/null; E=$(cut -d. -f1 /proc/uptime); echo P3_64MB_TOOK_$((E-S))SEC',
    'S=$(cut -d. -f1 /proc/uptime); dd if=/dev/mmcblk0p2 of=/dev/null bs=1M count=64 2>/dev/null; E=$(cut -d. -f1 /proc/uptime); echo P2_64MB_TOOK_$((E-S))SEC'
)
& 'd:\work\git_dev\k230_prj\k230_llm\.tools\send_com4.ps1' -Cmds $cmds -WaitSec 30
