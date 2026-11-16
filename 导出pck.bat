@echo off
setlocal
rem One-click export: builds C# (copies dll to game dir) and writes the pck. Close the game first.

set "GODOT=D:\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"
set "OUT=D:\Steam\steamapps\common\Slay the Spire 2\mods\TouhouAncients\TouhouAncients.pck"

pushd "%~dp0"
"%GODOT%" --headless --path "%CD%" --export-pack "Windows Desktop" "%OUT%"
set "RC=%ERRORLEVEL%"
popd

if not "%RC%"=="0" (
    echo [FAILED] export failed, exit code %RC%
    if /i not "%~1"=="-nopause" pause
    exit /b %RC%
)

echo [OK] %OUT%
if /i not "%~1"=="-nopause" pause
