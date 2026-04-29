@echo off
set GODOT="D:\Github\gemuera\tools\godot\Godot_v4.3-stable_mono_win64\Godot_v4.3-stable_mono_win64.exe"
set TESTS_DIR=D:\Github\gemuera\tests
set LOG_DIR=D:\Github\gemuera\logs

if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do set TS=%%i
set LOG_FILE=%LOG_DIR%\run_%TS%.log

echo [Gemuera] log: %LOG_FILE%

%GODOT% --verbose --log-file "%LOG_FILE%" --path "D:\Github\gemuera\godot" -- --game-root "%TESTS_DIR%"
