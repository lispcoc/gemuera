# Gemuera Remaining Stub-Only Implementations

Last updated: 2026-04-30

This document tracks areas that are still stub-only (no-op / placeholder / fixed return value) in the Godot port.

## 1) Console bridge stubs

File: `godot/src/Core/GameView/EmueraConsole.cs`

- `ConsoleWindow` / `HotkeyStateStub` / `ConsolePictureBox` are compatibility placeholders.
- `PrintHTMLIsland()` / `ClearHTMLIsland()` are empty.
- Background APIs are currently no-op or fixed-false:
  - `AddBackgroundImage()`
  - `RemoveBackground()`
  - `ClearBackgroundImage()`
  - `CBG_Clear()`
  - `CBG_ClearRange()`
  - `CBG_ClearButton()`
  - `CBG_ClearBMap()`
  - `CBG_SetGraphics()`
  - `CBG_SetButtonMap()`
  - `CBG_SetImage()` returns `false`
  - `CBG_SetButtonImage()` returns `false`
- Tooltip APIs are all no-op:
  - `SetToolTipColor()`
  - `SetToolTipDelay()`
  - `SetToolTipDuration()`
  - `SetToolTipFontName()`
  - `SetToolTipFontSize()`
  - `SetToolTipFormat()`
  - `SetToolTipImg()`
  - `CustomToolTip()`
- `PrintStringBuffer` remains a minimal stub: `IsEmpty => true`.

Impact:
- Script/UI compatibility is preserved at compile/runtime level.
- Features depending on island HTML buffering, advanced CBG composition, tooltip behavior, or print-buffer semantics are incomplete.

## 2) Image pipeline stubs

File: `godot/src/Core/GameView/ImageStubs.cs`

- `GraphicsImage` draw operations are no-op (`GDrawString`, `GDrawLine`, `GFillRectangle`, `GDrawG*`, etc.).
- `CroppedImage` and `SpriteAnime` are placeholders (`IsCreated => false`, no frame logic).
- `AppContents` is mostly stub:
  - `LoadContents()` returns `null`.
  - `GetSprite()` returns `null`.
  - sprite create/dispose APIs are no-op.
  - `SpriteDisposeAll()` returns `0`.

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

## 7) Sound state stub

File: `godot/src/Core/Runtime/Utils/Sound.cs`

- Playback delegation exists (`SoundManager` -> `IGameConsole`).
- `isPlaying()` is still fixed `false`.

Impact:
- Scripts relying on playback-state polling may behave differently.

## 8) Plugin manager partial placeholders

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
2. `EmueraConsole` CBG/image/button-map behavior
3. `PrintStringBuffer` semantics parity
4. `Sound.isPlaying()` real state integration
5. Optional: plugin placeholder methods
