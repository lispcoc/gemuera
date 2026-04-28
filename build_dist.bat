@echo off
setlocal enabledelayedexpansion

:: ============================================================
:: Gemuera — 配布ファイル生成バッチ
:: 使い方: build_dist.bat [windows] [web] [android] [all]
::   引数なし / all → 全プラットフォームをビルド
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
    echo [ERROR] Godot が見つかりません: %GODOT%
    exit /b 1
)

if not exist "%EXPORT_PRESETS%" (
    echo [WARN] export_presets.cfg が見つかりません。
    echo        Godot エディターで以下のプリセット名を作成してから再実行してください:
    echo          - Windows Desktop
    echo          - Web
    echo          - Android
    echo        %EXPORT_PRESETS%
    echo.
    echo        Godot エディターを起動するには open_godot.bat を実行してください。
    exit /b 1
)

:: --- dotnet ビルド ---
echo.
echo ========================================
echo  C# ビルド (dotnet build)
echo ========================================
pushd "%PROJECT_DIR%"
dotnet build Gemuera.csproj -c Release
if errorlevel 1 (
    echo [ERROR] dotnet build に失敗しました。
    popd
    exit /b 1
)
popd
echo [OK] C# ビルド完了

:: --- dist フォルダ準備 ---
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

:: ============================================================
:: Windows Desktop
:: ============================================================
if "%BUILD_WINDOWS%"=="1" (
    echo.
    echo ========================================
    echo  エクスポート: Windows Desktop
    echo ========================================
    set OUT_DIR=%DIST_DIR%\windows
    if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

    "%GODOT%" --headless --path "%PROJECT_DIR%" ^
        --export-release "Windows Desktop" "!OUT_DIR!\Gemuera.exe"
    if errorlevel 1 (
        echo [ERROR] Windows エクスポートに失敗しました。
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
    echo  エクスポート: Web (HTML5/WASM^)
    echo ========================================
    set OUT_DIR=%DIST_DIR%\web
    if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

    "%GODOT%" --headless --path "%PROJECT_DIR%" ^
        --export-release "Web" "!OUT_DIR!\index.html"
    if errorlevel 1 (
        echo [ERROR] Web エクスポートに失敗しました。
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
    echo  エクスポート: Android
    echo ========================================
    set OUT_DIR=%DIST_DIR%\android
    if not exist "!OUT_DIR!" mkdir "!OUT_DIR!"

    "%GODOT%" --headless --path "%PROJECT_DIR%" ^
        --export-release "Android" "!OUT_DIR!\Gemuera.apk"
    if errorlevel 1 (
        echo [ERROR] Android エクスポートに失敗しました。
    ) else (
        echo [OK] Android: !OUT_DIR!\Gemuera.apk
    )
)

echo.
echo ========================================
echo  完了: %DIST_DIR%
echo ========================================
dir /b "%DIST_DIR%"

endlocal
