@echo off
setlocal enabledelayedexpansion

:: ============================================================
:: Gemuera - Distribution build script
:: Usage: build_dist.bat [windows] [web] [android] [all]
::   No args / all -> build all platforms
:: ============================================================

set GODOT=D:\Github\gemuera\tools\godot\Godot_v4.3-stable_mono_win64\Godot_v4.3-stable_mono_win64.exe
set PROJECT_DIR=D:\Github\gemuera\godot
set DIST_DIR=D:\Github\gemuera\dist
set EXPORT_PRESETS=%PROJECT_DIR%\export_presets.cfg

:: --- 引数解析 ---
set BUILD_WINDOWS=0
set BUILD_WEB=0
set BUILD_ANDROID=0

if "%~1"=="" goto build_all
if /i "%~1"=="all" goto build_all

:parse_args
if "%~1"=="" goto check_presets
if /i "%~1"=="windows"  set BUILD_WINDOWS=1
if /i "%~1"=="web"      set BUILD_WEB=1
if /i "%~1"=="android"  set BUILD_ANDROID=1
shift
goto parse_args

:build_all
set BUILD_WINDOWS=1
set BUILD_WEB=1
set BUILD_ANDROID=1

:check_presets
:: --- 前提チェック ---
if not exist "%GODOT%" (
    echo [ERROR] Godot not found: %GODOT%
    exit /b 1
)

if not exist "%EXPORT_PRESETS%" (
    echo [WARN] export_presets.cfg not found.
    echo        Please create the following presets in Godot Editor then re-run:
    echo          - Windows Desktop
    echo          - Web
    echo          - Android
    echo        %EXPORT_PRESETS%
    echo.
    echo        To open Godot Editor, run open_godot.bat
    exit /b 1
)

:: --- dotnet build ---
echo.
echo ========================================
echo  C# Build (dotnet build)
echo ========================================
pushd "%PROJECT_DIR%"
dotnet build Gemuera.csproj -c Release
if errorlevel 1 (
    echo [ERROR] dotnet build failed.
    popd
    exit /b 1
)
popd
echo [OK] C# build succeeded

:: --- dist folder ---
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

:: ============================================================
:: Windows Desktop
:: ============================================================
if "%BUILD_WINDOWS%"=="1" (
    echo.
    echo ========================================
    echo  Export: Windows Desktop
    echo ========================================
    set OUT_DIR=%DIST_DIR%\windows
    if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

    "%GODOT%" --headless --path "%PROJECT_DIR%" ^
        --export-release "Windows Desktop" "!OUT_DIR!\Gemuera.exe"
    if errorlevel 1 (
        echo [ERROR] Windows export failed.
    ) else (
        echo [OK] Windows: !OUT_DIR!\Gemuera.exe
    )
)

:: ============================================================
:: Web (HTML5 / WASM)
:: ============================================================
if "%BUILD_WEB%"=="1" (
    echo.
    echo ========================================
    echo  Export: Web (HTML5/WASM)
    echo ========================================
    set OUT_DIR=%DIST_DIR%\web
    if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

    "%GODOT%" --headless --path "%PROJECT_DIR%" ^
        --export-release "Web" "!OUT_DIR!\index.html"
    if errorlevel 1 (
        echo [ERROR] Web export failed.
    ) else (
        echo [OK] Web: !OUT_DIR!\index.html
    )
)

:: ============================================================
:: Android
:: ============================================================
if "%BUILD_ANDROID%"=="1" (
    echo.
    echo ========================================
    echo  Export: Android
    echo ========================================
    set OUT_DIR=%DIST_DIR%\android
    if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

    "%GODOT%" --headless --path "%PROJECT_DIR%" ^
        --export-release "Android" "!OUT_DIR!\Gemuera.apk"
    if errorlevel 1 (
        echo [ERROR] Android export failed.
    ) else (
        echo [OK] Android: !OUT_DIR!\Gemuera.apk
    )
)

echo.
echo ========================================
echo  Done: %DIST_DIR%
echo ========================================
dir /b "%DIST_DIR%"

endlocal
