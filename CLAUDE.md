# CLAUDE.md — Gemuera プロジェクト状態ガイド

> このファイルはClaudeが自動的に読み込み、プロジェクトの現在の状態・構造・作業ルールを把握するためのものです。

---

## プロジェクト概要

**Gemuera** は、Windows専用の ERA ゲームインタープリター **Emuera**（C#/WinForms）を  
**Godot 4 (C#)** に移植して **Android・Webブラウザ（HTML5/WASM）** で動作させるプロジェクト。

- **元のプロジェクト**: `original/Emuera/` — .NET 10 / WinForms / GDI+ (読み取り専用・変更しない)
- **移植先プロジェクト**: `godot/` — Godot 4.3 / .NET 8 / C#

---

## リポジトリ構成

```
d:\Github\gemuera\
├── CLAUDE.md               ← このファイル
├── TODO.md                 ← 9フェーズの詳細な移植計画（必ず確認すること）
├── original/               ← 読み取り専用。変更禁止
│   ├── Emuera/             ← 元のC#ソースコード (136ファイル)
│   └── docs/               ← eraBasic言語仕様ドキュメント
└── godot/                  ← Godotプロジェクト（作業対象）
    ├── project.godot
    ├── Gemuera.csproj      ← .NET 8, Godot.NET.Sdk 4.3.0
    ├── Gemuera.sln
    ├── assets/
    │   └── icon.svg
    ├── scenes/
    │   ├── Main.tscn       ← エントリーポイント
    │   └── Console.tscn    ← コンソールUI
    └── src/
        ├── Bridge/         ← インタープリター↔Godot UI の接合層
        ├── Core/           ← 移植済みインタープリターコア
        └── UI/             ← Godot UIノードスクリプト
```

---

## アーキテクチャ

```
[ERAゲームファイル (.erb/.erh/.csv)]
         ↓ ファイル読み込み
[Core: Script Interpreter]
  Process (4 partial files)
    ├─ Process.cs            ← 初期化・メインループ
    ├─ Process.ScriptProc.cs ← 命令実行ループ
    ├─ Process.State.cs      ← コールスタック管理
    └─ Process.SystemProc.cs ← ゲームフェーズ状態機械
  Parser: LexicalAnalyzer, LogicalLineParser
  Loader: ErbLoader, ErhLoader
  Statements: Instruction (約100の命令), VariableData, ExpressionMediator
         ↓ IGameConsole インターフェース (Bridge層)
[UI: ConsoleNode.cs]         ← Godot RichTextLabel + LineEdit
[UI: MainNode.cs]            ← インタープリターをバックグラウンドスレッドで起動
```

### レイヤーごとの役割

| レイヤー | 場所 | 説明 |
|---|---|---|
| **Bridge** | `src/Bridge/` | `IGameConsole` インターフェース、`StringStyle`、`InputResult` 型 |
| **Core** | `src/Core/` | ERBスクリプト解釈器（Windowsに依存しないC#コード） |
| **UI** | `src/UI/` | Godotノード実装。`IGameConsole` を実装 |

---

## 現在の進捗状況

### ✅ フェーズ 1 — 基盤構築（完了）

**`dotnet build` ビルド成功 — 0 エラー / 0 警告**

元の Emuera コードを3つの方法で `godot/src/Core/` に配置済み：

1. **そのままコピー** — Windows依存なし（Parser, Data, Statements 大半）
2. **適応済み** — `System.Drawing.Color` → `EraColor`、`EmueraConsole` → `IGameConsole` に置換
3. **スタブ実装** — Windows専用機能をno-opまたは移譲で置き換え

#### 適応済みの主要ファイル

| オリジナル | Gemuera版での変更点 |
|---|---|
| `System.Drawing.Color` | `EraColor` シム (`src/Core/EraColor.cs`) |
| `EmueraConsole` | `IGameConsole` インターフェース |
| `Runtime/Config/Config.cs` | EraColor使用、WinForms・Dialog除去 |
| `Runtime/Config/ConfigData.cs` | 同上 |
| `Runtime/Config/ConfigItem.cs` | 同上 |
| `Runtime/Script/Loader/ErbLoader.cs` | IGameConsole参照 |
| `Runtime/Script/Loader/ErhLoader.cs` | IGameConsole参照 |
| `Runtime/Script/Process*.cs` (4ファイル) | IGameConsole参照 |
| `Runtime/Script/Statements/ArgumentBuilder.cs` | IGameConsole参照 |
| `Runtime/Script/Statements/Instraction.Child.cs` | 同上 |
| `Runtime/Script/Statements/Function/Creator*.cs` | 同上 |
| `Runtime/Script/Statements/Variable/VariableEvaluator.cs` | 同上 |
| `Runtime/Utils/EvilMask/Shape.cs` | EraColor使用 |
| `Runtime/Utils/EvilMask/Utils.cs` | EraColor使用 |

#### Windowsスタブ

| ファイル | 元の依存 | Gemuera実装 |
|---|---|---|
| `Utils/SoundManager.cs` | WMPLib COM / NAudio | IGameConsoleに委譲 |
| `Utils/WinmmTimer.cs` | `winmm.dll` P/Invoke | `Environment.TickCount64` |
| `Utils/WinInput.cs` | Win32 keyboard hooks | no-op (`GetKeyState`返り値0) |
| `Utils/WebPWrapper.cs` | `libwebp.dll` P/Invoke | Godot組込みWebP使用予定 |
| `Utils/Sys.cs` | `System.Windows.Forms` | WinForms除去、`#if GODOT` 分岐 |
| `Utils/PluginSystem/PluginManager.cs` | `Assembly.LoadFrom()` | シングルトン、空リスト返却 |
| `Utils/Sound.cs` | WMPLib/NAudio 再生 | `isPlaying()` / `setVolume()` stub |

#### 新規作成ファイル

| ファイル | 説明 |
|---|---|
| `src/Core/EraColor.cs` | System.Drawing.Color の軽量代替（`FromName`含む） |
| `src/Core/Program.cs` | パス設定（`ExeDir`/`CsvDir`/`ErbDir`/`SavDir`/`ContentDir`/`DatDir`/`SoundDir`） |
| `src/Core/GlobalStatic.cs` | WindowsForm依存を除いた版 |
| `src/Core/DrawingCompat.cs` | `System.Drawing.*` 全域スタブ（Color/Font/Graphics/Pen/Brush/Rectangle等） |
| `src/Core/GameView/EmueraConsole.cs` | IGameConsoleラッパー。ERA解釈器が直接呼ぶ全APIを実装 |
| `src/Core/GameView/UIGameTypes.cs` | `HtmlManager`/`ConsoleDisplayLine`等スタブ |
| `src/Core/GameView/ImageStubs.cs` | `AbstractImage`/`GraphicsImage`/`ASprite`/`AppContents`等スタブ |
| `src/Bridge/IGameConsole.cs` | Print/Input/Sound/CBG/GetWindowTitle 等の抽象IF |
| `src/Bridge/DisplayPackets.cs` | StringStyle, InputResult 型 |
| `src/UI/ConsoleNode.cs` | RichTextLabel+LineEditによる実装 |
| `src/UI/MainNode.cs` | インタープリター起動・管理 |
| `scenes/Main.tscn` | メインシーン |
| `scenes/Console.tscn` | コンソールUIシーン |

### 🔲 フェーズ 2〜9 — 未着手

詳細は [TODO.md](./TODO.md) を参照。

> **次のステップ**: フェーズ 2 — `EmueraConsole.Print*` → `IGameConsole` → Godot `RichTextLabel` へのリアルタイム表示パイプラインの実装。

---

## 作業ルール

### 必ず守ること

1. **`original/` は読み取り専用** — 元のEmueraコードは変更しない
2. **`godot/` 配下のみ編集** — 移植作業はすべてここ
3. **参照先確認** — コアコードの変更前は `original/Emuera/` の対応ファイルを読む
4. **フェーズ管理** — 新しい実装は TODO.md の該当フェーズのチェックを更新する
5. **ゲームデータは変更しない** — `tests/` 以下のゲームデータ（ERB/CSV等）は読み取り専用。動作上の不具合はプログラム側（`godot/src/`）を修正して解決する

### 名前空間

| 場所 | 名前空間 |
|---|---|
| `src/Core/` | `MinorShift.Emuera.*` (元のまま保持) |
| `src/Bridge/` | `Gemuera.Bridge` |
| `src/UI/` | `Gemuera.UI` |

### `EmueraConsole` → `IGameConsole` のパターン

元コードの `EmueraConsole console` 引数はすべて `IGameConsole console` に置換済み。  
新しいスクリプト命令追加時は同様に `IGameConsole` 経由で出力する。

### `System.Drawing.Color` の扱い

コアコード内では `EraColor` を使用する。  
Godot UI側(`src/UI/`)では `Godot.Color` を使用する。  
`ConsoleNode.cs` の `GodotColorFromArgb(int argb)` で相互変換。

### スレッドモデル

- インタープリター（`Process`）: **バックグラウンドスレッド**（`Task.Run()`）
- GodotのUI更新: **メインスレッド**（`ConsoleNode._uiQueue` に `Action` を enqueue）
- 入力待ち: `TaskCompletionSource<InputResult>` で async/await

---

## よく参照するファイル

| 目的 | ファイル |
|---|---|
| 命令一覧 | `original/Emuera/Runtime/Script/Statements/Instraction.Child.cs` |
| 組み込み関数一覧 | `original/Emuera/Runtime/Script/Statements/BuiltInFunctionCode.cs` |
| 変数コード一覧 | `original/Emuera/Runtime/Script/Statements/Variable/VariableCode.cs` |
| コンソールAPI | `original/Emuera/UI/Game/EmueraConsole.cs` + `EmueraConsole.Print.cs` |
| ゲームループ | `original/Emuera/Runtime/Script/Process.SystemProc.cs` |
| 設定項目一覧 | `original/Emuera/Runtime/Config/ConfigCode.cs` |
| eraBasic言語仕様 | `original/docs/docs/Emuera/` |

---

## ビルド方法

```bash
# Godot 4.3 が必要
# プロジェクトを開く
godot --path d:/Github/gemuera/godot

# C#ビルドのみ (dotnet)
cd d:/Github/gemuera/godot
dotnet build Gemuera.csproj
```

---

## 未解決の技術的課題

1. **`Process.Run()` メソッド** — `MainNode.cs` が呼ぶが未実装。`Process.cs` に public な Run() ループを追加する必要あり（フェーズ 2 で実装予定）
2. **`Process.RequestQuit()` メソッド** — 同様に未実装
3. **`AppContents`** — `ImageStubs.cs` にスタブ実装済み。ただし実際の画像キャッシュ・デコードはフェーズ 6 で実装予定
4. **`PrintStringBuffer`** — `EmueraConsole.cs` に `IsEmpty=true` のスタブのみ。本来の行バッファ管理はフェーズ 2 で実装予定
5. **`EscapedParts`** — `Dictionary<long, List<AConsoleDisplayNode>>` の空実装。CBG/divパーツ表示はフェーズ 2/6 で実装予定
6. **`CtrlZ`** — `Config.Config.Ctrl_Z_Enabled` を参照。Ctrl+Z機能はGodot版では後回し
7. **ファイルアクセス** — Android/Web では `System.IO.File` が使えない。Phase 4 で対応予定
8. **`Sound.isPlaying()`** — 常に `false` を返すスタブ。実際のサウンド再生はフェーズ 5 で実装予定
9. **`IGameConsole.GetWindowTitle()`** — `Engine.GetVersionInfo()` を返すプレースホルダー。正式実装はフェーズ 2 で

---

## 関連ドキュメント

- [TODO.md](./TODO.md) — フェーズ別詳細タスクリスト
- [Godot 4 C# docs](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/)
- [eraBasic仕様](./original/docs/docs/Emuera/)
