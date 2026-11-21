@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

echo ============================================
echo   招亦废丽罗 · 律令之楔  Unity 引擎版
echo ============================================
echo.

set "PROJ=%~dp0UnityEdition"
if not exist "%PROJ%\Assets" (
  echo [错误] 未找到 UnityEdition 工程目录。
  pause
  exit /b 1
)

rem ---- 1) 查找本机 Unity Editor（Unity Hub 默认安装位置） ----
set "UNITY="
if exist "%ProgramFiles%\Unity\Hub\Editor" (
  for /f "delims=" %%D in ('dir /b /ad /o-n "%ProgramFiles%\Unity\Hub\Editor" 2^>nul') do (
    if exist "%ProgramFiles%\Unity\Hub\Editor\%%D\Editor\Unity.exe" (
      set "UNITY=%ProgramFiles%\Unity\Hub\Editor\%%D\Editor\Unity.exe"
      goto :found
    )
  )
)
if exist "%ProgramFiles%\Unity 2022.3.62f1\Editor\Unity.exe" set "UNITY=%ProgramFiles%\Unity 2022.3.62f1\Editor\Unity.exe"
if exist "%ProgramFiles%\Unity\Editor\Unity.exe" set "UNITY=%ProgramFiles%\Unity\Editor\Unity.exe"

:found
if defined UNITY (
  echo [启动] Unity Editor: "%UNITY%"
  echo [工程] "%PROJ%"
  start "" "%UNITY%" -projectPath "%PROJ%"
  goto :eof
)

rem ---- 2) 没找到 Editor：尝试启动 Unity Hub ----
set "HUB="
if exist "%ProgramFiles%\Unity Hub\Unity Hub.exe" set "HUB=%ProgramFiles%\Unity Hub\Unity Hub.exe"
if exist "%LOCALAPPDATA%\Programs\Unity Hub\Unity Hub.exe" set "HUB=%LOCALAPPDATA%\Programs\Unity Hub\Unity Hub.exe"

if defined HUB (
  echo [提示] 未找到 Unity Editor，已尝试打开 Unity Hub。
  echo        请在 Hub 中点击 Add - Add project from disk，选择：
  echo        "%PROJ%"
  start "" "%HUB%"
  goto :eof
)

echo [未安装] 本机没有检测到 Unity Editor / Unity Hub。
echo.
echo 安装步骤：
echo   1. https://unity.com/download 下载 Unity Hub
echo   2. 在 Hub 中安装任意 Unity 2022.3 LTS 版本（推荐 2022.3.62f1）
echo   3. 打开 Hub - Add project from disk - 选择：
echo      "%PROJ%"
echo   4. 点击 Play 即可游玩（工程内已自动生成 Main 场景与全部内容）
echo.
pause
