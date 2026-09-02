# diag_card.ps1 - READ-ONLY hex forensics: what is actually on the card at key offsets
$ErrorActionPreference = 'Stop'
$out = "d:\work\git_dev\k230_prj\k230_llm\.tools\card_diag.txt"
"start $(Get-Date -Format HH:mm:ss)" | Out-File $out -Encoding utf8
function Log($s) { $s | Out-File $out -Append -Encoding utf8 }
trap {
    Log ("FATAL: " + $_.Exception.Message)
    exit 3
}
function DumpAt($label, $byteOffset, $len) {
    $fd.Position = $byteOffset
    $b = New-Object byte[] $len
    $off = 0
    while ($off -lt $len) {
        $n = $fd.Read($b, $off, $len - $off)
        if ($n -le 0) { break }
        $off += $n
    }
    Log ("== " + $label + " @ " + $byteOffset + " ==")
    $hex = ($b[0..63] | ForEach-Object { $_.ToString('X2') }) -join ' '
    Log ("  first64: " + $hex)
    $ascii = -join ($b[0..63] | ForEach-Object { if ($_ -ge 32 -and $_ -le 126) { [char]$_ } else { '.' } })
    Log ("  ascii64: " + $ascii)
    $a5count = ($b | Where-Object { $_ -eq 0xA5 }).Count
    $zcount = ($b | Where-Object { $_ -eq 0 }).Count
    $fcount = ($b | Where-Object { $_ -eq 0xFF }).Count
    Log ("  stats" + $len + "B: A5=" + $a5count + " 00=" + $zcount + " FF=" + $fcount)
}

$fd = [IO.File]::Open('\\.\PhysicalDrive1','Open','Read','ReadWrite')
DumpAt "LBA0 (partition sector)" 0 4096
DumpAt "LBA1 (GPT header)" 512 4096
DumpAt "128MB (rootfs start)" 134217728 4096
DumpAt "4MB (block1 start)" 4194304 4096
DumpAt "2GB" 2147483648 4096
$fd.Close()
Log "DIAG DONE"
Get-Content $out
