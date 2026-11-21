# 数据冒烟测试：用 Unity 桩 + 真实 StreamingAssets JSON 验证 Data.cs 的解析与世界数学
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tools/checks/data_smoke.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$out = Join-Path $env:TEMP "dotnet8"
$dn = Join-Path $out "dotnet.exe"
$csc = Join-Path $out "sdk\8.0.404\Roslyn\bincore\csc.dll"
$refDir = Join-Path $out "packs\Microsoft.NETCore.App.Ref\8.0.11\ref\net8.0"
$stubDir = Join-Path $root "tools\checks\stubcheck"
$target = Join-Path $env:TEMP "DataSmoke.dll"
$rsp = Join-Path $env:TEMP "datasmoke.rsp"
$refs = Get-ChildItem $refDir -Filter *.dll | ForEach-Object { "-r:$($_.FullName)" }
$data = Join-Path $root "UnityEdition\Assets\Scripts\Data.cs"
$lines = @("-nologo", "-target:exe", "-langversion:9", "-nostdlib+", "-noconfig", "-nowarn:CS2023",
  "-out:$target") + $refs + @((Join-Path $stubDir "UnityStubs.cs"), $data, (Join-Path $stubDir "DataSmoke.cs"))
Set-Content -Path $rsp -Value $lines -Encoding UTF8
& $dn $csc "@$rsp" | Out-Host
if ($LASTEXITCODE -ne 0) { Write-Host "[FAIL] 编译失败"; exit 1 }

$cfg = Join-Path $env:TEMP "DataSmoke.runtimeconfig.json"
Set-Content -Path $cfg -Encoding UTF8 -Value '{ "runtimeOptions": { "tfm": "net8.0", "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" } } }'
$streaming = Join-Path $root "UnityEdition\Assets\StreamingAssets"
& $dn exec --runtimeconfig $cfg $target $streaming | Out-Host
exit $LASTEXITCODE
