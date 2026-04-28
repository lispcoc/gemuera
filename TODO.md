# Gemuera — Godot移植 TODO

Emuera（Windowsのみ対応C#/WinFormsゲームインタープリター）を  
Godot 4 (C#) に移植して **Android・ブラウザ（HTML5）** で動かす計画。

---

## 移植先プロジェクト

```
d:\Github\gemuera\
├── original/       ← 元のEmuera（読み取り専用・変更しない）
├── godot/          ← 新Godotプロジェクト（移植先）
└── TODO.md         ← 本ファイル
```

---

## アーキテクチャ概要

```
[ERA Scripts (.erb/.erh/.csv)]
        ↓ load
[Script Interpreter Core]  ← 元のC#コードをほぼそのまま流用
  Process / Parser / Loader / Statements / VariableData
        ↓ IGameConsole interface
[Godot UI Layer]           ← WinFormsを完全に置き換え
  ConsoleNode (RichTextLabel + LineEdit)
  MainNode (シーンルート)
```

### レイヤー構成 (`godot/src/`)

| フォルダ | 内容 | 状態 |
|---|---|---|
| `Core/` | ポータブルな解釈器コア（System.Drawing不使用） | 移植中 |
| `Core/Runtime/Config/` | コンフィグ読み込み | 要Color型置換 |
| `Core/Runtime/Script/` | ERBパーサー・ローダー・実行エンジン | ほぼそのまま |
| `Core/Runtime/Utils/` | ユーティリティ（SFMT, Encoding等） | ほぼそのまま |
| `Bridge/` | `IGameConsole` インターフェース | 実装済み |
| `UI/` | Godot固有のUIスクリプト | 実装中 |
| `scenes/` | Godotシーン(.tscn) | 実装中 |

---

## フェーズ 1 — 基盤構築 ✅ 完了

**`dotnet build` ビルド成功: 0 エラー / 0 警告** (2026-04-28 確認)

- [x] コードベース調査・アーキテクチャ把握
- [x] 移植計画書（本ファイル）作成
- [x] Godotプロジェクト骨格作成 (`project.godot`, `Gemuera.csproj`, `Gemuera.sln`)
- [x] `IGameConsole` インターフェース定義 (`src/Bridge/IGameConsole.cs`) — `GetWindowTitle()`含む
- [x] `DisplayPackets.cs` — 表示データ型定義 (`StringStyle`, `InputResult`)
- [x] `EraColor.cs` — System.Drawing.Color置換シム（`FromName()`含む）
- [x] `DrawingCompat.cs` — `System.Drawing.*` 全域スタブ（Color/Font/Graphics/Pen/Brush/ColorMatrix/StringFormat/SizeF等）
- [x] `Program.cs` — `ExeDir/CsvDir/ErbDir/SavDir/ContentDir/DatDir/SoundDir` パス設定
- [x] `GlobalStatic.cs` — IGameConsole参照化、PrivateFontCollection除去
- [x] コア移植: `InputRequest.cs`
- [x] コア移植: `Runtime/Utils/` ポータブルファイル (SFMT, EncodingHandler等)
- [x] コア移植: `Runtime/Utils/` Windowsスタブ (Sound メソッド化/setVolume追加, WebP, WinInput, WinmmTimer, Sys)
- [x] コア移植: `Runtime/Config/` (System.Drawing → EraColor, WinForms除去)
- [x] コア移植: `Runtime/Script/Data/` (全ファイル)
- [x] コア移植: `Runtime/Script/Parser/` (全ファイル)
- [x] コア移植: `Runtime/Script/Loader/` (EmueraConsole → IGameConsole)
- [x] コア移植: `Runtime/Script/Statements/` (全ファイル、MessageBox/DialogResult除去等)
- [x] コア移植: `Process*.cs` — EmueraConsole → IGameConsole差し替え
- [x] `GameView/EmueraConsole.cs` — IGameConsoleラッパー（PrintBuffer/EscapedParts/Await/LineCount/IsTimeOut等含む）
- [x] `GameView/UIGameTypes.cs` — HtmlManagerスタブ (HtmlLength/HtmlSubString等)
- [x] `GameView/ImageStubs.cs` — AbstractImage/GraphicsImage/ASprite/AppContentsスタブ
- [x] `PluginSystem/PluginManager.cs` — シングルトン、HasMethod/GetMethodスタブ
- [x] `PluginSystem/IPluginMethod.cs` — PluginMethodParameterBuilderスタブ追加
- [x] Godot UIスタブ: `ConsoleNode.cs` (RichTextLabel + LineEdit)
- [x] Godot UIスタブ: `MainNode.cs` (インタープリター起動・管理)
- [x] Godotシーン: `Main.tscn`, `Console.tscn`


## フェーズ 2 — コンソール表示の実装

- [x] `ConsoleNode`: `RichTextLabel`によるスタイル付き文字表示 (ToBbcode / color / bold / italic / underline / strike)
- [x] `ConsoleNode`: スクロールバック実装 (ScrollContainer + scroll_following)
- [x] `ConsoleNode`: `DRAWLINE` (区切り線) 実装 (動的幅対応)
- [x] `ConsoleNode`: ボタン文字列クリック (`PRINTBUTTON`) 実装 (MetaClicked + URL修正)
- [x] `ConsoleNode`: `PrintC`/`PrintCR` 中央/右揃え BBCode対応
- [x] `ConsoleNode`: `HTML_PRINT` → BBCode変換 (基本タグ)
- [x] `ConsoleNode`: `PRINT_IMG` / 画像表示 (BBCode [img]タグ + 幅・高さ・揃え対応)
- [x] `EmueraConsole`: LineCount / LastLineIsEmpty / IsTimeOut 追跡実装
- [x] CBG (クライアント背景画像) 対応 (`TextureRect` + `CbgContainer`)
- [ ] フォント設定 (Godot `FontFile` 読み込み / CJKフォント同梱)
- [ ] 文字幅計算の実装 (`Font.get_string_size()`)

## フェーズ 3 — 入力処理の実装

- [x] テキスト入力 (`INPUT`/`INPUTS`) — `LineEdit`
- [x] `WAIT`/`WAITANYKEY` — Enter/クリック待ち (InputType.EnterKey / AnyKey)
- [x] `ONEINPUT` — 1文字入力 (OneInput フラグ対応)
- [x] タイムアウト付き入力 (`TINPUT`/`TINPUTS`) (Timelimit / IsTimeOut / IsTimeout)
- [x] ボタン番号選択 (`INPUT` with button display) (OnMetaClicked)
- [ ] マウス入力 (`MOUSE_*`) 対応
- [ ] ファンクションキー・マクロ機能

## フェーズ 4 — ファイルI/O・セーブロード

- [x] Android/Web: セーブデータディレクトリを `user://sav` にリダイレクト (`Program.SetSavDir`)
- [x] Android/Web: ERB/CSV ファイルを Godot `DirAccess`/`FileAccess` 経由でプリロード (`PreloadGodotDir`)
- [ ] `emuera.config` 読み込み (Android/Web: `user://` 対応)
- [ ] セーブデータの読み書き (バイナリ) — 動作確認
- [ ] _Replace.csv / _Rename.csv 適用

## フェーズ 5 — サウンド

- [x] BGM再生 (ogg/mp3) — `AudioStreamPlayer` + `AudioStreamOggVorbis`/`AudioStreamMP3` (ループ対応)
- [x] SE (効果音) 再生 — `AudioStreamPlayer`
- [x] WAV再生対応 (`AudioStreamWav`)
- [ ] フェード対応
- [x] WMPLib・NAudio依存の完全除去 (`SoundManager` → `IGameConsole` 経由)
- [x] BGM/SE ボリューム制御 (`SetBgmVolume`/`SetSeVolume` → `AudioStreamPlayer.VolumeDb`)
- [ ] フェード対応

## フェーズ 6 — 画像・WebP対応

- [x] 標準画像 (png/jpg/bmp/webp) — `Image.Load()` + `ImageTexture` (`LoadTexture` ヘルパー)
- [x] CBG背景画像表示 (`TextureRect` + `CbgSet`/`CbgClear`)
- [ ] `AppContents` の画像キャッシュをGodot版に移植
- [ ] アニメーションスプライト対応
- [ ] WebP → Godot組込みデコーダー (Godot 4はWebP対応)
- [ ] `AppContents` の画像キャッシュをGodot版に移植
- [ ] アニメーションスプライト対応

## フェーズ 7 — Androidビルド

- [ ] Godot Android エクスポート設定
- [ ] ファイルアクセスを `ProjectSettings.globalize_path()` 経由に
- [ ] ゲームデータの同梱 (res://) または実行時DL
- [ ] タッチ入力対応 (LineEditはそのまま動くはず)
- [ ] Android固有権限設定 (ストレージ読み書き)
- [ ] APKビルド & テスト

## フェーズ 8 — Webブラウザビルド

- [ ] Godot HTML5 エクスポート設定
- [ ] セーブデータ → `IndexedDB` (Godotが自動対応)
- [ ] ゲームデータのパック (`*.pck`)
- [ ] スレッド制限への対応 (SharedArrayBuffer なしの環境)
  - WASMスレッドが使えない場合コルーチン化が必要
- [ ] Webフォント対応
- [ ] ブラウザでのテスト

## フェーズ 9 — 品質・互換性

- [ ] 既存ERAゲームとの互換性テスト
- [ ] `EMUERA_*` 定数の正確な模倣
- [ ] デバッグ機能 (DEBUG変数、debugprint)
- [ ] PluginSystem の代替 (GDExtension or 無効化)
- [ ] パフォーマンスプロファイリング

---

## 置換マッピング表

| Emuera (Windows) | Gemuera (Godot) |
|---|---|
| `System.Windows.Forms` | Godotシーンツリー |
| `System.Drawing.Graphics` | `RichTextLabel` / `CanvasItem._Draw()` |
| `System.Drawing.Color` | `Godot.Color` |
| `System.Drawing.Font` | `Godot.Font` / `FontFile` |
| `Graphics.MeasureString()` | `Font.get_string_size()` |
| `WMPLib` (COM) | `AudioStreamPlayer` |
| `NAudio` | `AudioStreamPlayer` |
| `libwebp.dll` P/Invoke | Godot組込みWebPデコーダー |
| `winmm.dll timeGetTime` | `Time.get_ticks_msec()` |
| `WinInput.cs` | `Input.is_action_pressed()` |
| `PrivateFontCollection` | `FontFile` (res://assets/fonts/) |
| `Assembly.LoadFrom()` プラグイン | GDExtension (要検討) |
| `Application.Run()` イベントループ | Godot `_Process()` + signals |
| `RichTextBox` (入力欄) | `LineEdit` / `TextEdit` |
| `EraPictureBox` (描画領域) | `SubViewport` + `Control` |
| `VScrollBar` | `ScrollContainer` |

---

## 既知の課題・制限

1. **スレッドモデル**  
   元のEmuera: UI + 解釈器が同一スレッド (WinForms メッセージポンプ)  
   Godot移植: 解釈器をバックグラウンドスレッドで動かし `CallDeferred` で UI更新  
   → Web環境ではWASMスレッド非対応の場合にコルーチン化が必要

2. **Shift-JIS**  
   `System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` は  
   Godot/Android/Web でも動作するはず。要確認。

3. **ファイルシステム**  
   Android/Web: `File.ReadAllText(path)` は使えない。  
   → `FileAccess` (Godot API) に切り替えるか、起動時に全ERBをメモリにロードする方式へ

4. **`unsafe` コード / libwebp P/Invoke**  
   Web (WASM) では `unsafe` ブロックのP/Invokeは動作しない。  
   → GodotのネイティブWebPサポートを使う

5. **プラグインシステム**  
   Android/Web では `Assembly.LoadFrom()` は基本的に不可。  
   → プラグイン機能は Desktop専用に限定するか代替策を検討

---

## 参考資料

- [Godot 4 C# API](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/)
- [Godot Android Export](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html)
- [Godot Web Export](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html)
- [eraBasic言語仕様](original/docs/docs/Emuera/)
