@echo off
set GODOT="D:\Github\gemuera\tools\godot\Godot_v4.3-stable_mono_win64\Godot_v4.3-stable_mono_win64.exe"
set TESTS_DIR=D:\Github\gemuera\tests

%GODOT% --path "D:\Github\gemuera\godot" -- --game-root "%TESTS_DIR%"
