# Gemuera

Emuera（ERAゲームインタープリター）を Godot 4 (C#) に移植し、Android・Web ブラウザ（WASM）で動作させるプロジェクト。

## 必要環境

| ツール | バージョン |
|---|---|
| [Godot 4 Mono](https://godotengine.org/download/) | 4.3-stable (mono) |
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 以上 |
| Android SDK (Android ビルド時のみ) | — |

Godot の実行ファイルは `tools/godot/` に配置済みです。

---

## Godot エディターを開く

```bat
open_godot.bat
```

初回起動時にエクスポートテンプレートのインストールが必要です。  
**Editor → Manage Export Templates → Download and Install**

---

## C# のみビルド確認

```bat
cd godot
dotnet build Gemuera.csproj
```

---

## 配布ビルド (build_dist.bat)

```bat
build_dist.bat [windows] [web] [android] [all]
```

| 引数 | 説明 |
|---|---|
| `windows` | Windows Desktop 向け `.exe` |
| `web` | HTML5/WASM 向け `index.html` |
| `android` | Android 向け `.apk` |
| `all` または省略 | 全プラットフォーム |

出力先: `dist\<platform>\`

### 例

```bat
:: Web のみビルド
build_dist.bat web

:: 全プラットフォーム
build_dist.bat all
```

### 事前準備

Godot エクスポートテンプレートが未インストールの場合、エクスポートは失敗します。  
`open_godot.bat` でエディターを開き、テンプレートをインストールしてから実行してください。

---

## ディレクトリ構成

```
gemuera/
├── godot/          ← Godot プロジェクト（作業対象）
├── original/       ← 元の Emuera ソース（読み取り専用）
├── tools/          ← Godot 実行ファイル
├── dist/           ← ビルド出力（自動生成）
├── open_godot.bat  ← エディター起動
└── build_dist.bat  ← 配布ビルド
```
