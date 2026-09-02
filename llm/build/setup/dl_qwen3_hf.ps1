# dl_qwen3_hf.ps1 - download Qwen3-0.6B original HF weights from hf-mirror
$base = 'https://hf-mirror.com/Qwen/Qwen3-0.6B/resolve/main'
$dst = 'd:\work\git_dev\k230_prj\k230_llm\models\Qwen3-0.6B-hf'
New-Item -ItemType Directory -Force -Path $dst | Out-Null
$files = @('config.json', 'generation_config.json', 'tokenizer.json', 'tokenizer_config.json', 'model.safetensors')
foreach ($f in $files) {
    Write-Output ("downloading " + $f)
    $code = curl.exe -sL --retry 5 --retry-delay 3 --max-time 1800 -C - -o "$dst\$f" -w "%{http_code}" "$base/$f"
    $size = 0; if (Test-Path "$dst\$f") { $size = (Get-Item "$dst\$f").Length }
    Write-Output ("  http=$code size=$([math]::Round($size/1MB,2))MB")
}
Write-Output "HF_DOWNLOAD_DONE"
