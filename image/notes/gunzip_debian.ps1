# .NET 解压 debian 镜像 gz -> img
$src = "d:\yilei.wang\k230_prj\k230_llm\downloads\06_images\linux\CanMV-K230_01studio_debian_v1.2_nncase_v2.11.0_bd411da2.img.gz"
$dst = $src -replace '\.gz$',''
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$in = [System.IO.File]::OpenRead($src)
$gz = New-Object System.IO.Compression.GZipStream($in, [System.IO.Compression.CompressionMode]::Decompress)
$out = [System.IO.File]::Create($dst)
$buf = New-Object byte[] (4MB)
while (($n = $gz.Read($buf, 0, $buf.Length)) -gt 0) { $out.Write($buf, 0, $n) }
$out.Close(); $gz.Close(); $in.Close()
"decompressed: {0:N0} MB in {1:N0}s" -f ((Get-Item $dst).Length/1MB), $sw.Elapsed.TotalSeconds
