# Unity C# 静态编译检查（无 Unity Editor 环境下用 Roslyn + 自写 UnityEngine/UnityEditor 桩校验脚本）
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tools/checks/compile_check.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$out = Join-Path $env:TEMP "dotnet8"
$dn = Join-Path $out "dotnet.exe"
$csc = Join-Path $out "sdk\8.0.404\Roslyn\bincore\csc.dll"
$refDir = Join-Path $out "packs\Microsoft.NETCore.App.Ref\8.0.11\ref\net8.0"
if (-not (Test-Path $dn)) { throw "dotnet 未找到：$dn" }
if (-not (Test-Path $csc)) { throw "Roslyn csc 未找到：$csc" }
$refs = Get-ChildItem $refDir -Filter *.dll | ForEach-Object { "-r:$($_.FullName)" }
$stubDir = Join-Path $root "tools\checks\stubcheck"
$scripts = Get-ChildItem (Join-Path $root "UnityEdition\Assets\Scripts\*.cs") | ForEach-Object { $_.FullName }
$editor = Join-Path $root "UnityEdition\Assets\Editor\ProjectSetup.cs"

function Invoke-Csc([string[]]$sources, [string[]]$defines, [string]$targetName) {
  $target = Join-Path $env:TEMP $targetName
  $rsp = Join-Path $env:TEMP ($targetName + ".rsp")
  $lines = @("-nologo", "-target:library", "-langversion:9", "-nostdlib+", "-noconfig",
    "-nowarn:CS2023", "-define:$($defines -join ';')", "-out:$target") + $refs + $sources
  Set-Content -Path $rsp -Value $lines -Encoding UTF8
  & $dn $csc "@$rsp" | Out-Host
  return $LASTEXITCODE
}

Write-Host "== 运行时脚本 =="
$code1 = Invoke-Csc (@((Join-Path $stubDir "UnityStubs.cs")) + $scripts) @("UNITY_2022_3_OR_NEWER") "StubCheck.Runtime.dll"

Write-Host "`n== 编辑器脚本（ProjectSetup） =="
$code2 = Invoke-Csc (@((Join-Path $stubDir "UnityStubs.cs"), (Join-Path $stubDir "UnityEditorStubs.cs")) + $scripts + @($editor)) @("UNITY_EDITOR", "UNITY_2022_3_OR_NEWER") "StubCheck.Editor.dll"

if ($code1 -eq 0 -and $code2 -eq 0) { Write-Host "`n[OK] Unity 脚本编译通过（运行时 + 编辑器，0 error）"; exit 0 }
Write-Host "`n[FAIL] 编译错误，见上"; exit 1
