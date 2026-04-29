@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%" >nul

echo [INFO] Cleaning generated test files under tests\...

if exist "gemuera_runtime.log" (
  del /q "gemuera_runtime.log"
  echo [OK] Removed gemuera_runtime.log
)

if exist "sav\*.sav" (
  del /q "sav\*.sav"
  echo [OK] Removed sav\*.sav
)

if exist "dat\*.dat" (
  del /q "dat\*.dat"
  echo [OK] Removed dat\*.dat
)

if exist "crash\*.log" del /q "crash\*.log"
if exist "crash\*.txt" del /q "crash\*.txt"
if exist "crash\*.dmp" del /q "crash\*.dmp"
echo [OK] Removed crash dump/log files if present

echo [INFO] Cleaning game data contents under tests\...
call :CleanFolderContents "csv" "csv"
call :CleanFolderContents "erb" "erb"
call :CleanFolderContents "resources" "resources"
call :CleanFolderContents "sound" "sound"
call :CleanFolderContents "dat" "dat"
call :CleanFolderContents "Plugins" "Plugins"

echo [DONE] Test cleanup completed.

popd >nul
endlocal
goto :eof

:CleanFolderContents
set "TARGET_DIR=%~1"
set "TARGET_LABEL=%~2"

if not exist "%TARGET_DIR%\" (
  echo [OK] %TARGET_LABEL% not found, skipped
  goto :eof
)

for /r "%TARGET_DIR%" %%F in (*) do (
  set "FILE_NAME=%%~nxF"
  if /I not "!FILE_NAME!"=="README.md" if /I not "!FILE_NAME!"==".gitkeep" del /q "%%F" >nul 2>&1
)

for /f "delims=" %%D in ('dir "%TARGET_DIR%" /ad /b /s ^| sort /r') do rd "%%D" >nul 2>&1

echo [OK] Cleaned %TARGET_LABEL% contents
goto :eof
