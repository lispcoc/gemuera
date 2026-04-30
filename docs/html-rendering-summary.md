# HTML描画方針まとめ（2026-04-30）

## 目的
- Emuera互換のHTML描画で発生しやすい崩れ・回帰を防止する。
- BBCodeだけに依存せず、必要に応じて専用描画へ切り替える設計を固定する。

## 適用範囲
- 実装対象: `godot/src/UI/ConsoleNode.cs`
- 関連アダプタ: `godot/src/Core/GameView/EmueraConsole.cs`

## 基本方針（ハイブリッド）
1. 前処理（Pre-pass）
- `<div rect=...><img ...></div>` のようなCBG専用要素を先に抽出し、CBG描画へ渡す。

2. 解析（HtmlToOps）
- 残りHTMLを描画操作列へ変換する。
- 現在の操作型:
  - `TextOp`（テキスト。BBCodeエスケープ・エンティティデコード済み）
  - `InlineImageOp`（インライン画像。src/width/height）
- 今後追加予定:
  - `ButtonOp` / `BlockAlignOp` / `SpanStyleOp` / `RubyOp`

3. 描画（DrawOps）
- `TextOp` は `RichTextLabel.AppendText` で描画。
- `InlineImageOp` は `RichTextLabel.AddImage` で描画。
- BBCodeで意味がずれる要素は専用オペレーションとして描画する。

## ルーティング規約
- BBCodeで安定表現できるもののみBBCode経由にする。
- 表現差異や不安定要素は専用RenderOpへ逃がす。
- `HTML_PRINT` に人工的な末尾改行を追加しない（原文・タグ由来のみ）。

## 現在の対応状況
### 対応済み
- `<br>`
- `<b>`, `<i>`, `<u>`, `<s>/<strike>`
- `<font color=...>`
- `<font size=...>` → `[font_size=N]`
- `<a href='...'>` のリンク変換
- `<button value=...>`, `<nonbutton>`
- `<p>/<div align='center|right'>`
- `<img ...>`（インライン: `InlineImageOp`）
- `<div rect=...><img ...></div>`（CBG前処理）
- `<div rect=...>text/buttons...</div>`（位置指定 RichTextLabel オーバーレイ: `PrintHtmlDiv`）
- `HTML_PRINT opt` パラメータ（opt=false で末尾改行付与）
- `depth`/`border`/`bcolor`/`padding` 属性の基本反映（`PrintHtmlDiv`）

### 未対応（今後）
- `<font size=...>`, `<font face=...>`
- `<span style=...>` のCSS解釈
- `<ruby>`, `<rt>`, `<rp>`
- タグ不整合時の厳密復旧
- `border` / `padding` / `depth` などレイアウト属性
- `HTML_PRINT(..., opt=1)` のバッファ意味論の原作準拠

## 直近の実装差分
- `ConsoleNode.PrintHtml` を `HtmlToOps -> DrawOps` へ移行開始。
- `ParseInlineHtmlRenderOps` を追加し、`TextOp` と `InlineImageOp` を導入。
- `HTML_PRINT` の強制末尾改行は廃止済み。

## 回帰チェック項目
- テキストと画像が混在した `HTML_PRINT` で順序が崩れない。
- button/nonbutton メニューがクリック可能で表示も崩れない。
- `p/div align` の中央・右寄せが維持される。
- 画像失敗ログに「元src」と「解決後パス」が出る。

## 運用ルール
HTML互換機能を追加・変更する場合は、必ず以下を実施する。
1. HtmlToOpsに変換規則を追加/更新する。
2. DrawOpsに対応描画を追加する。
3. 実データまたは再現スクリプトで1ケース以上検証する。
4. 本ドキュメントとTODOを更新する。
