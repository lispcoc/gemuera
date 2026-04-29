# tests — テスト用ERAゲームデータ

`run_game.bat` 実行時にこのフォルダのリソースが使われます。

## フォルダ構成

```
tests/
├── csv/        ← CSVデータファイル (.csv)
├── erb/        ← ERBスクリプトファイル (.erb / .erh)
├── Plugins/    ← プラグインDLL配置先 (Windowsデスクトップ実行時のみ)
├── sav/        ← セーブデータ（自動生成）
├── resources/  ← 画像・フォントなどのリソース
├── sound/      ← BGM・効果音
├── dat/        ← KDMMフォーマットの変数データ (.dat)
└── clean_test_files.bat ← テスト生成物のクリーンアップ
```

## 使い方

1. テストしたいERAゲームファイルを各フォルダに配置する
2. リポジトリルートで `run_game.bat` を実行する

## プラグインの配置

- テスト用プラグインDLLは `tests/Plugins/` 直下に配置する
- 現在の実装では `*.dll` のみ読み込み対象

## テスト生成物のクリーン

- `tests/clean_test_files.bat` を実行すると、以下の生成物を削除する
- `tests/gemuera_runtime.log`
- `tests/sav/*.sav`
- `tests/dat/*.dat`
- `tests/crash/*.log`, `tests/crash/*.txt`, `tests/crash/*.dmp`

## ゲームデータのクリーン

- `tests/clean_test_files.bat` はゲームデータ本体の中身も削除する
- 削除対象フォルダ: `tests/csv`, `tests/erb`, `tests/resources`, `tests/sound`, `tests/dat`, `tests/Plugins`
- フォルダ構造は残し、`README.md` と `.gitkeep` は保持する
