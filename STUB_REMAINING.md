# Gemuera Remaining Stub-Only Implementations

Last updated: 2026-04-30

This document tracks areas that are still stub-only (no-op / placeholder / fixed return value) in the Godot port.

## 1) Console bridge stubs

File: `godot/src/Core/GameView/EmueraConsole.cs`

- `ConsoleWindow` / `HotkeyStateStub` / `ConsolePictureBox` are compatibility placeholders.
- `PrintHTMLIsland()` / `ClearHTMLIsland()` are empty.
- Background APIs are partially bridged:
  - `AddBackgroundImage()` / `RemoveBackground()` / `ClearBackgroundImage()` now rebuild via `IGameConsole.CbgSet/CbgClear`
  - `CBG_Clear()` / `CBG_ClearRange()` / `CBG_ClearButton()` now track layer metadata (depth/button) and clear selectively
  - `CBG_ClearBMap()` now resets button-map state
  - `CBG_SetButtonMap()` now forwards a file-backed map to UI and `INPUTMOUSEKEY` can sample map RGB into `RESULT:5`
  - `CBG_SetGraphics()` now works when the source `GraphicsImage` carries a file-backed bitmap path
  - `CBG_SetImage()` / `CBG_SetButtonImage()` now succeed for resource-backed sprites (`AppContents.GetSprite`経由)
- Tooltip APIs are all no-op:
  - `SetToolTipColor()`
  - `SetToolTipDelay()`
  - `SetToolTipDuration()`
  - `SetToolTipFontName()`
  - `SetToolTipFontSize()`
  - `SetToolTipFormat()`
  - `SetToolTipImg()`
  - `CustomToolTip()`
- `PrintStringBuffer` now mirrors line-emptiness (`IsEmpty` reflects current line state) but full original buffer semantics are still未移植.

Impact:
- Script/UI compatibility is preserved at compile/runtime level.
- Features depending on island HTML buffering, CBG button-map hover/tooltip parity, tooltip behavior, or print-buffer semantics are incomplete.

## 2) Image pipeline stubs

File: `godot/src/Core/GameView/ImageStubs.cs`

- `GraphicsImage` draw operations are still no-op (`GDrawString`, `GDrawLine`, `GFillRectangle`, `GDrawG*`, etc.).
- `GraphicsImage.GCreateFromF()` now clones minimal bitmap metadata instead of holding caller-owned bitmap instances.
- `CroppedImage` / `SpriteAnime` now keep minimal created/size state, but frame rendering logic is未実装.
- `AppContents` now has a minimal registry:
  - `GetSprite()` resolves files under `Program.ContentDir` and returns resource-backed sprites.
  - `CreateSpriteG()` / `CreateSpriteAnime()` / `SpriteDispose()` / `SpriteDisposeAll()` are functional at registry level.
  - Full image decode/caching parity with Emuera is still未実装.

Impact:
- Basic compile compatibility exists, but full sprite/image-cache behavior from Emuera is not yet ported.

## 3) UI game type compatibility stubs

File: `godot/src/Core/GameView/UIGameTypes.cs`

- `StringMeasure.MeasureString()` returns `0f`.
- `ConsoleDisplayLine.DrawTo()` is no-op.
- `HtmlManager` is simplified (not full HTML parser behavior).

Impact:
- Core logic can run, but legacy line drawing/measurement and full HTML helper parity are incomplete.

## 4) Drawing compatibility shim is intentionally no-op

File: `godot/src/Core/DrawingCompat.cs`

- Provides `System.Drawing*` compatibility types for build portability.
- `Graphics` drawing methods are no-op by design.

Impact:
- Works as a compile bridge only; not a rendering backend.

## 5) Input simulation stub

File: `godot/src/Core/Runtime/Utils/WinInput.cs`

- `SendKey()` and `SendString()` are no-op.
- `GetKeyState()` always returns `0`.

Impact:
- Win32-style synthetic key input is not supported.

## 6) WebP wrapper decode stub

File: `godot/src/Core/Runtime/Utils/WebPWrapper.cs`

- `IsAvailable` is `true`, but `Decode()` throws `NotSupportedException`.
- Current strategy is to decode WebP through Godot image APIs in UI layer.

Impact:
- Any code path expecting direct `WebPWrapper.Decode()` support remains unsupported.

## 7) Plugin manager partial placeholders

File: `godot/src/Core/Runtime/Utils/PluginSystem/PluginManager.cs`

- `SetParent(...)` is empty.
- Built-in LLM plugin methods (`CALL_GEMINI`, `CALL_OLLAMA`, `CALL_GENERIC_LLM_API`) are explicit no-op placeholders.

Impact:
- Plugin surface exists, but these methods currently do not execute real functionality.

## Notes

- This list intentionally excludes ordinary TODO comments that are design/refactor notes but not active stubs.
- Desktop Windows plugin DLL loading logic is implemented (best-effort load with exception suppression), but some methods are still placeholders as noted above.

## Suggested next implementation order

1. `ImageStubs.cs` / `AppContents` parity (cache + sprite behavior)
2. `PrintStringBuffer` full semantics parity
3. `EmueraConsole` CBG hover/tooltip parity
4. Optional: plugin placeholder methods
