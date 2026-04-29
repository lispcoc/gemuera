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
- [x] フォント設定 (Godot `SystemFont` によるCJKフォント適用 + `ApplyConfigFont()` をMainNodeから呼び出し)
- [ ] フォントファイル同梱 (Godot `FontFile` 読み込み / CJKフォント同梱 — Android/Web向け)
- [x] 文字幅計算の実装 (`Font.get_string_size()`)

### HTML-BBCode 変換カバレッジ確認 (2026-04-30)

対象実装: `godot/src/UI/ConsoleNode.cs` (`PrintHtml`, `HtmlToBbcode`, `ExtractDivImageInstructions`)

- [x] タグ名の大小文字混在を許容 (`<BR>`, `<Font>` 等)
- [x] テキスト部分を BBCode エスケープして表示崩れを回避 (`[` を `[lb]` へ)
- [x] 改行タグ: `<br>` / `<br/>` / `<br />`
- [x] 文字装飾タグ: `<b>`, `<i>`, `<u>`, `<s>`, `<strike>`
- [x] 色タグ: `<font color='...'>` / `</font>` (`#RRGGBB`, `#AARRGGBB`)
- [x] 画像タグ: `<img src=... width=... height=...>` (quoted/unquoted 属性)
- [x] 属性値の `px` 単位を寸法として解釈
- [x] CBG向け拡張: `<div rect='x,y,w,h'><img ...></div>` を抽出して `CbgSet` へ転送
- [x] 未対応/不要タグは除去し、内側のテキストは保持

未カバー/今後対応:

- [ ] `<font>` の `size` / `face` 属性
- [ ] `<span style='...'>` の CSS 解釈
- [x] `<a href='...'>` のリンク変換
- [ ] `<ruby>`, `<rt>`, `<rp>` 等の日本語組版タグ
- [ ] ネスト不整合タグに対する厳密な復旧ルール（現状は素通し/除去ベース）
- [x] HTMLエンティティ (`&nbsp;`, `&lt;`, `&#NNNN;`) のデコード
- [ ] レイアウト系属性 (`padding`, `border`, `depth`, `align`) の本格反映

## フェーズ 3 — 入力処理の実装

- [x] テキスト入力 (`INPUT`/`INPUTS`) — `LineEdit`
- [x] `WAIT`/`WAITANYKEY` — Enter/クリック待ち (InputType.EnterKey / AnyKey)
- [x] `ONEINPUT` — 1文字入力 (OneInput フラグ対応)
- [x] タイムアウト付き入力 (`TINPUT`/`TINPUTS`) (Timelimit / IsTimeOut / IsTimeout)
- [x] ボタン番号選択 (`INPUT` with button display) (OnMetaClicked)
- [x] マウス入力 (`MOUSE_*`/`INPUTMOUSEKEY`) 対応 (PrimitiveMouseKey — `_Input` ハンドラーでキャッチ、InputResult.MouseType/Button/X/Y/Count)
- [x] ファンクションキー・マクロ機能

## フェーズ 4 — ファイルI/O・セーブロード

- [x] Android/Web: セーブデータディレクトリを `user://sav` にリダイレクト (`Program.SetSavDir`)
- [x] Android/Web: ERB/CSV ファイルを Godot `DirAccess`/`FileAccess` 経由でプリロード (`PreloadGodotDir`)
- [x] `emuera.config` 読み込み (Android/Web: `user://` 対応)
- [ ] セーブデータの読み書き (バイナリ) — 動作確認
- [x] _Replace.csv / _Rename.csv 適用 (Android/Webでは Preload キャッシュ経由にフォールバック)

## フェーズ 5 — サウンド

- [x] BGM再生 (ogg/mp3) — `AudioStreamPlayer` + `AudioStreamOggVorbis`/`AudioStreamMP3` (ループ対応)
- [x] SE (効果音) 再生 — `AudioStreamPlayer`
- [x] WAV再生対応 (`AudioStreamWav`)
- [x] フェード対応
- [x] WMPLib・NAudio依存の完全除去 (`SoundManager` → `IGameConsole` 経由)
- [x] BGM/SE ボリューム制御 (`SetBgmVolume`/`SetSeVolume` → `AudioStreamPlayer.VolumeDb`)
- [x] `Sound.isPlaying()` の再生状態連携 (`SoundManager` → `IGameConsole.IsBgmPlaying/IsSePlaying`)
- [x] フェード対応

## フェーズ 6 — 画像・WebP対応

- [x] 標準画像 (png/jpg/bmp/webp) — `Image.Load()` + `ImageTexture` (`LoadTexture` ヘルパー)
- [x] CBG背景画像表示 (`TextureRect` + `CbgSet`/`CbgClear`)
- [x] WebP → Godot組込みデコーダー (`LoadTexture` 内の `Image.Load()` が WebP を自動処理)
- [ ] `AppContents` の画像キャッシュをGodot版に移植
- [ ] アニメーションスプライト対応

### 画像トラブル対策 TODO (追加: 2026-04-30)

- [ ] `HTML_PRINT` の `<img>` は BBCode `[img]` 非依存を維持し、`RichTextLabel.AddImage()` 経路に統一（回帰防止テスト追加）
- [ ] `PRINT_IMG` の描画経路も BBCode依存を見直し、外部パス/Android/Web で安定する直接テクスチャ描画へ統一検討
- [ ] `IMG_LINE_10001` など実行時スプライト名の解決: `AppContents` で生成した画像を `ConsoleNode` 側で直接参照できる橋渡しを実装
- [ ] `GraphicsImage` の no-op 描画API (`GDraw*`, `GFillRectangle`, `GDrawString`) を段階的に実装し、スクリプト生成画像が実際に表示される状態にする
- [ ] `AppContents` に実画像キャッシュ（ロード済みテクスチャ再利用、破棄タイミング管理）を実装し、長時間プレイでの再ロード/メモリ肥大を抑制
- [ ] `CBG_SetButtonMap` / `INPUTMOUSEKEY` のヒットテストを実ゲームデータで検証し、RGB取得・透明判定・座標系のズレをテストで固定化
- [ ] `div/img` 属性互換を拡張（`xpos`/`ypos`/`rect`/`width`/`height` のMixedNum換算に加え、`srcb`/`srcm`/`display`/`depth` の扱いを仕様化）
- [ ] 画像読み込み失敗時のログを整理し、`src`・解決後パス・呼び出し元命令 (`HTML_PRINT`/`PRINT_IMG`/`CBG*`) を必ず出力
- [ ] Webエクスポート向けに `res://` / `user://` / 実ファイルパスの許容範囲を定義し、環境別の画像ロード戦略を文書化

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

## フェーズ 9 — コントローラーサポート ✅ 実装済み

マウス・キーボードなしでも基本操作を可能にするゲームパッド対応。

- [x] **選択肢ナビゲーション** — D-pad ↑↓ で `[url=N]` 選択肢を循環選択、入力欄とステータスバーに反映
- [x] **決定ボタン (A)** — IntValue / IntButton / StrValue / StrButton / AnyValue 入力を確定、WAIT / WAITANYKEY / AnyKey を進める
- [x] **キャンセル / WAIT 進行 (B)** — WAIT / WAITANYKEY / AnyKey を進める
- [x] **スクロール (D-pad ↑↓)** — 選択肢なし時はスクロールとして動作 (80px/step)
- [x] **ページスクロール (L1/R1)** — 1画面分スクロール
- [x] **右スティック縦軸** — `_Process()` で毎フレーム連続スクロール
- [x] **INPUTMOUSEKEY 対応 (A)** — キー入力として MouseType=4 を返す
- [x] **ステータスバーヒント** — 選択肢ナビ中は `▶ N (↑↓ 選択 / A 決定)` を表示
- [x] **大量選択肢の安定化** — 選択肢抽出・ハイライトに安全上限を導入し、大規模メニューでのクラッシュを回避
- [ ] タッチスクリーン仮想キーボード連携 (Android Phase 7 で対応予定)
- [ ] INPUTS (文字列) 向け仮想キーボード / OSK 呼び出し

## フェーズ 10 — 品質・互換性

- [ ] 既存ERAゲームとの互換性テスト
- [x] `EMUERA_VERSION` 定数 → `"1.824.0.0"` を返すトークンで実装済み
- [x] `WINDOW_TITLE` 変数 — `SetWindowTitle()` / `GetWindowTitle()` 実装済み (`_windowTitle` フィールドで追跡)
- [ ] デバッグ機能 (DEBUG変数、debugprint)
- [ ] PluginSystem の代替 (GDExtension or 無効化)
- [ ] パフォーマンスプロファイリング

## フェーズ 11 — 設定画面

Godot 版 Gemuera にゲーム内設定 UI を追加する。元の Emuera の ConfigDialog (WinForms) に相当。

- [x] `SettingsNode.cs` — Godot Control スクリプト (TabContainer + 各設定項目)
- [x] `Settings.tscn` — CanvasLayer オーバーレイシーン
- [x] `ConsoleNode` から ESC キーで設定画面を開く / 閉じる (入力待ち中は無視)
- [x] 「表示」タブ: フォントサイズ (SpinBox), フォント名 (LineEdit), 文字色/背景色/選択色 (ColorPickerButton)
- [x] 「サウンド」タブ: BGM 音量 / SE 音量 (HSlider, 0-100)
- [x] 「システム」タブ: MaxLog (SpinBox), DisplayReport (CheckBox)
- [x] 設定を `emuera.config` に保存 (`ConfigData.SaveConfig(string path)` オーバーロード追加、`TrySaveEraConfig()` 経由で常に明示的パスへ保存), ゲームに即時反映
- [x] Android/Web 向け: 設定ファイル保存先を `user://emuera.config` へ (`TrySaveEraConfig()` が `ProjectSettings.GlobalizePath("user://emuera.config")` を明示的パスとして使用、`Program.ExeDir` 変更なし)
- [x] 設定画面から BGM/SE ボリュームをリアルタイム反映 (スライダー操作で即時反映 + `ConsoleNode._Ready()` 内で `SettingsNode.LoadVolumeSetting()` を呼んで起動時復元)

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
