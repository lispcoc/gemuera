// Gemuera — Console UI Node
// Implements IGameConsole using Godot RichTextLabel + LineEdit.
// Attach to Console.tscn root Control node.
using Godot;
using Gemuera.Bridge;
using MinorShift.Emuera.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using MinorShift.Emuera;
using EraConfig = MinorShift.Emuera.Runtime.Config.Config;

namespace Gemuera.UI;

/// <summary>
/// Godot implementation of IGameConsole.
/// The ERA interpreter runs on a background thread; all Godot API calls
/// are dispatched to the main thread via CallDeferred / TaskCompletionSource.
/// </summary>
public partial class ConsoleNode : Control, IGameConsole
{
    // ----------------------------------------------------------------
    // Child node references (set via inspector or GetNode)
    // ----------------------------------------------------------------

    [Export] public NodePath ScrollContainerPath  = "VBox/Scroll";
    [Export] public NodePath RichTextLabelPath    = "VBox/Scroll/RichText";
    [Export] public NodePath InputLinePath        = "VBox/InputLine";
    [Export] public NodePath StatusLabelPath      = "VBox/StatusBar";
    [Export] public NodePath BgmPlayerPath        = "BgmPlayer";
    [Export] public NodePath SePlayerPath         = "SePlayer";
    [Export] public NodePath CbgContainerPath     = "CbgContainer";
    [Export] public NodePath SettingsPath         = "Settings";

    // ----------------------------------------------------------------
    // Runtime fields
    // ----------------------------------------------------------------

    private RichTextLabel  _richText;
    private LineEdit        _inputLine;
    private Label           _statusBar;
    private ScrollContainer _scroll;
    private AudioStreamPlayer _bgmPlayer;
    private AudioStreamPlayer _sePlayer;
    private string _currentBgmPath = null;
    private Control _cbgContainer;
    private Image _cbgButtonMapImage;
    private volatile bool _bgmPlayingState;
    private volatile bool _sePlayingState;

    // Colours (packed ARGB)
    private int _fgArgb = unchecked((int)0xFFC0C0C0); // default light gray
    private int _bgArgb = unchecked((int)0xFF000000);
    private TextStyleFlags _styleFlags = TextStyleFlags.Normal;

    // Font size / metrics (updated by ApplyConfigFont)
    private int _fontSize = 18;
    private Font _activeFont;     // stored after ApplyConfigFont, used for char-width measurement
    private float _charWidth = 9; // width of a single half-width character in pixels

    // Current function-key macro group (0 = default; Shift+F1..F10 cycles 0..9)
    private int _macroGroup = 0;

    // Current input request
    private TaskCompletionSource<InputResult> _inputTcs;
    private InputRequest _pendingRequest;

    // Output queue (interpreter thread → UI thread)
    private readonly ConcurrentQueue<Action> _uiQueue = new();

    // Tracks how much of _bbcodeAccum was present when the last INPUT was set up.
    // ConvertNumberPatterns is applied only to content added after this position.
    private int _lastInputPos = 0;

    // Parallel accumulator that mirrors every AppendText call so we can read back
    // the raw BBCode (RichTextLabel.Text is NOT updated by AppendText).
    private readonly StringBuilder _bbcodeAccum = new();

    // Regex to detect existing [url=…]…[/url] blocks (skip these during auto-linkify).
    private static readonly Regex _urlTagRegex =
        new(@"\[url=[^\]]*\].*?\[/url\]",
            RegexOptions.Singleline | RegexOptions.Compiled);
    // Match [lb]N] (with optional leading spaces/NBSPs and optional suffix inside the bracket).
    // Captures: group 1 = spaces before number, group 2 = number,
    //            group 3 = suffix inside bracket (e.g. "↑"), group 4 = trailing text on the same option.
    private static readonly Regex _bracketNumRegex =
        new(@"\[lb\]([ \u00a0]*)(\d+)([^\]\n]*)\]((?:(?!\[lb\])(?!\[/)(?!\n).)*)",
            RegexOptions.Compiled);
    // Extract [url=VALUE] values from BBCode for controller choice navigation.
    private static readonly Regex _urlValueRegex =
        new(@"\[url=([^\]]+)\]", RegexOptions.Compiled);
    // Match a complete [url=VALUE]…[/url] block for highlight replacement.
    private static readonly Regex _urlFullRegex =
        new(@"\[url=([^\]]+)\](.*?)\[/url\]",
            RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex _imgTagRegex =
        new(@"<img\b([^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _divTagRegex =
        new(@"<div\b([^>]*)>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex _attrRegex =
        new("([a-zA-Z][a-zA-Z0-9_-]*)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))",
            RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex _stripHtmlTagRegex =
        new(@"</?(?:div|font|button|nonbutton|p|span|nobr)\\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] _imageExtCandidates =
        { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
    private static readonly Dictionary<string, string> _resolvedImagePathCache =
        new(StringComparer.OrdinalIgnoreCase);
    // Texture cache: keyed by the resolved path (or sprite name for AppContents sprites).
    private static readonly Dictionary<string, WeakReference<Texture2D>> _textureCache =
        new(StringComparer.OrdinalIgnoreCase);

    // ----------------------------------------------------------------
    // Controller / gamepad state
    // ----------------------------------------------------------------

    // Available selectable choices for the current INPUT (populated from [url=…] links).
    private readonly List<string> _controllerChoices = new();
    // Currently highlighted choice index (-1 = none highlighted yet).
    private int _controllerChoiceIndex = -1;
    // Whether menu highlight overlay is allowed for the current menu section.
    // Disabled for very large sections to avoid heavy regex replacement on each D-pad move.
    private bool _controllerHighlightEnabled;
    // True when controller choices were intentionally truncated for safety.
    private bool _controllerChoicesTruncated;

    // Safety limits for controller choice extraction/highlight.
    private const int MaxControllerChoices = 512;
    private const int MaxControllerMenuChars = 250_000;
    private const int MaxHighlightMenuChars = 120_000;

    // Safety limits for BBCode growth/linkify to avoid native RichTextLabel crashes.
    private const int MaxBbcodeAccumChars = 600_000;
    private const int TrimmedBbcodeChars = 450_000;
    private const int MaxRichTextResetChars = 220_000;
    private const int TargetRichTextResetChars = 160_000;
    // Keep this aligned with the BBCode trim threshold so menus remain clickable
    // until the same point where we already trim the accumulator for safety.
    private const int MaxLinkifyTotalChars = MaxBbcodeAccumChars;
    private const int MaxLinkifySectionChars = 180_000;

    // Position in _bbcodeAccum where the current menu section starts (for highlight overlay).
    private int _menuStartPos = -1;
    // Whether _richText.Text currently has a highlight overlay (differs from _bbcodeAccum).
    private bool _highlightActive;
    // URL value currently under the mouse cursor (null = no hover).
    private string _mouseHoverValue;

    // ----------------------------------------------------------------
    // Godot lifecycle
    // ----------------------------------------------------------------

    public override void _Ready()
    {
        _richText  = GetNode<RichTextLabel>(RichTextLabelPath);
        _inputLine = GetNode<LineEdit>(InputLinePath);
        _statusBar = GetNode<Label>(StatusLabelPath);
        _scroll    = GetNode<ScrollContainer>(ScrollContainerPath);

        _richText.BbcodeEnabled = true;
        // scroll_following = true is set in Console.tscn so the view follows new output.

        _inputLine.TextSubmitted += OnInputSubmitted;
        _inputLine.Editable = false; // disabled until INPUT command

        // Connect button (URL tag) click
        _richText.MetaClicked      += OnMetaClicked;
        _richText.MetaHoverStarted += OnMetaHoverStarted;
        _richText.MetaHoverEnded   += OnMetaHoverEnded;

        _bgmPlayer = GetNode<AudioStreamPlayer>(BgmPlayerPath);
        _sePlayer  = GetNode<AudioStreamPlayer>(SePlayerPath);
        _bgmPlayer.Finished += () =>
        {
            /* loop BGM */
            if (_bgmPlayer.Stream != null)
            {
                _bgmPlayer.Play();
                _bgmPlayingState = true;
            }
            else
            {
                _bgmPlayingState = false;
            }
        };
        _sePlayer.Finished += () => { _sePlayingState = false; };
        _cbgContainer = GetNode<Control>(CbgContainerPath);

        // Wire settings overlay
        if (!SettingsPath.IsEmpty)
        {
            var settingsNode = GetNodeOrNull<SettingsNode>(SettingsPath);
            if (settingsNode != null)
                SetSettingsNode(settingsNode);
        }

        // Restore persisted BGM/SE volumes from gemuera_settings.cfg
        int savedBgm = SettingsNode.LoadVolumeSetting("bgm_volume", 80);
        int savedSe  = SettingsNode.LoadVolumeSetting("se_volume",  80);
        SetUserBgmVolume(savedBgm);
        SetUserSeVolume(savedSe);
    }

    // Drain the UI action queue every frame
    public override void _Process(double delta)
    {
        while (_uiQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception e) { GD.PrintErr($"[ConsoleNode] UI action error: {e}"); }
        }

        // Right stick vertical: continuous scroll (works without focus).
        float rightY = Input.GetJoyAxis(0, JoyAxis.RightY);
        if (Mathf.Abs(rightY) > 0.25f && _scroll != null)
        {
            int scrollDelta = (int)(rightY * 500f * (float)delta);
            _scroll.ScrollVertical = Mathf.Max(0, _scroll.ScrollVertical + scrollDelta);
        }
    }

    // ----------------------------------------------------------------
    // Controller (joypad) input
    // ----------------------------------------------------------------

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventJoypadButton pad || !pad.Pressed) return;

        var req = _pendingRequest;
        var tcs = _inputTcs;
        bool hasPending = req != null && tcs != null && !tcs.Task.IsCompleted;

        switch (pad.ButtonIndex)
        {
            // A / Cross — confirm / advance
            case JoyButton.A:
                if (!hasPending) return;
                GetViewport().SetInputAsHandled();
                if (req.InputType == InputType.AnyKey || req.InputType == InputType.EnterKey)
                {
                    _inputLine.Editable = false;
                    _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
                    ClearControllerState();
                    tcs.TrySetResult(new InputResult { Value = "" });
                }
                else if (req.InputType == InputType.IntValue
                      || req.InputType == InputType.IntButton
                        || req.InputType == InputType.StrValue
                        || req.InputType == InputType.StrButton
                      || req.InputType == InputType.AnyValue)
                {
                    // Submit the value shown in the input line (updated by D-pad navigation).
                    ClearControllerState();
                    OnInputSubmitted(_inputLine.Text);
                }
                else if (req.InputType == InputType.PrimitiveMouseKey)
                {
                    _inputLine.Editable = false;
                    ClearControllerState();
                    tcs.TrySetResult(new InputResult { MouseType = 4, MouseButton = 0,
                        MouseX = (int)_lastMousePos.X, MouseY = (int)_lastMousePos.Y });
                }
                return;

            // B / Circle — advance for WAIT-type inputs (same as any key)
            case JoyButton.B:
                if (!hasPending) return;
                if (req.InputType == InputType.AnyKey || req.InputType == InputType.EnterKey)
                {
                    GetViewport().SetInputAsHandled();
                    _inputLine.Editable = false;
                    _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
                    ClearControllerState();
                    tcs.TrySetResult(new InputResult { Value = "" });
                }
                return;

            // D-pad Up — navigate to previous choice, or scroll up
            case JoyButton.DpadUp:
                GetViewport().SetInputAsHandled();
                if (_controllerChoices.Count > 0)
                {
                    _controllerChoiceIndex = (_controllerChoiceIndex <= 0
                        ? _controllerChoices.Count : _controllerChoiceIndex) - 1;
                    ApplyControllerChoice();
                }
                else if (_scroll != null)
                {
                    _scroll.ScrollVertical = Mathf.Max(0, _scroll.ScrollVertical - 80);
                }
                return;

            // D-pad Down — navigate to next choice, or scroll down
            case JoyButton.DpadDown:
                GetViewport().SetInputAsHandled();
                if (_controllerChoices.Count > 0)
                {
                    _controllerChoiceIndex = (_controllerChoiceIndex + 1) % _controllerChoices.Count;
                    ApplyControllerChoice();
                }
                else if (_scroll != null)
                {
                    _scroll.ScrollVertical += 80;
                }
                return;

            // L1 — page up
            case JoyButton.LeftShoulder:
                GetViewport().SetInputAsHandled();
                if (_scroll != null)
                    _scroll.ScrollVertical = Mathf.Max(0, _scroll.ScrollVertical - (int)_scroll.Size.Y);
                return;

            // R1 — page down
            case JoyButton.RightShoulder:
                GetViewport().SetInputAsHandled();
                if (_scroll != null)
                    _scroll.ScrollVertical += (int)_scroll.Size.Y;
                return;
        }
    }

    /// <summary>Set the input line text to the currently selected controller choice and update the status bar hint.</summary>
    private void ApplyControllerChoice()
    {
        if (_controllerChoiceIndex < 0 || _controllerChoiceIndex >= _controllerChoices.Count) return;
        string val = _controllerChoices[_controllerChoiceIndex];
        _inputLine.Text = val;
        _inputLine.CaretColumn = val.Length;
        string truncated = _controllerChoicesTruncated ? "  (一部省略)" : "";
        _statusBar.Text =
            $"[コントローラー] ▶  {val}  ({_controllerChoiceIndex + 1}/{_controllerChoices.Count})  ↑↓選択  Aで決定{truncated}";
        UpdateHighlight();
    }

    /// <summary>Populate controller choices from [url=…] tags found in the given BBCode section.</summary>
    private void ExtractControllerChoices(string bbSection)
    {
        _controllerChoices.Clear();
        _controllerChoiceIndex = -1;
        _controllerChoicesTruncated = false;
        _controllerHighlightEnabled = bbSection.Length <= MaxHighlightMenuChars;

        if (bbSection.Length > MaxControllerMenuChars)
            return;

        var seen = new HashSet<string>();
        for (Match m = _urlValueRegex.Match(bbSection); m.Success; m = m.NextMatch())
        {
            string val = UrlDecodeBb(m.Groups[1].Value);
            if (seen.Add(val))
            {
                _controllerChoices.Add(val);
                if (_controllerChoices.Count >= MaxControllerChoices)
                {
                    _controllerChoicesTruncated = true;
                    break;
                }
            }
        }
        if (_controllerChoices.Count > 0)
        {
            _controllerChoiceIndex = 0;
            ApplyControllerChoice();
        }
    }

    /// <summary>Reset controller navigation state and clear any hint from the status bar.</summary>
    private void ClearControllerState()
    {
        _controllerChoices.Clear();
        _controllerChoiceIndex = -1;
        _controllerChoicesTruncated = false;
        _controllerHighlightEnabled = false;
        _mouseHoverValue = null;
        _menuStartPos = -1;
        if (_statusBar != null) _statusBar.Text = "";
        UpdateHighlight(); // restores _richText.Text if a highlight overlay was active
    }

    /// <summary>
    /// Apply or remove the hover/selection highlight overlay on the menu section.
    /// Mouse hover takes precedence over controller navigation.
    /// Must be called on the Godot main thread.
    /// </summary>
    private void UpdateHighlight()
    {
        if (!_controllerHighlightEnabled)
        {
            if (_highlightActive)
            {
                _highlightActive = false;
                int savedScroll = _scroll?.ScrollVertical ?? 0;
                CompactBbcodeForRichTextReset();
                _richText.Text = _bbcodeAccum.ToString();
                if (_scroll != null) _scroll.ScrollVertical = savedScroll;
            }
            return;
        }

        // Mouse hover takes precedence; fall back to controller index
        string targetVal = _mouseHoverValue;
        if (targetVal == null && _controllerChoiceIndex >= 0 && _controllerChoiceIndex < _controllerChoices.Count)
            targetVal = _controllerChoices[_controllerChoiceIndex];

        if (targetVal == null || _menuStartPos < 0 || _menuStartPos > _bbcodeAccum.Length)
        {
            // No highlight needed — restore plain accumulator content if we had an overlay
            if (_highlightActive)
            {
                _highlightActive = false;
                int savedScroll = _scroll?.ScrollVertical ?? 0;
                CompactBbcodeForRichTextReset();
                _richText.Text = _bbcodeAccum.ToString();
                if (_scroll != null) _scroll.ScrollVertical = savedScroll;
            }
            return;
        }

        CompactBbcodeForRichTextReset();
        string fullBb  = _bbcodeAccum.ToString();
        string prefix  = _menuStartPos > 0 ? fullBb[.._menuStartPos] : "";
        string menu    = fullBb[_menuStartPos..];
        string capturedVal = targetVal; // avoid closure capture of mutable local

        string highlighted = _urlFullRegex.Replace(menu, m =>
        {
            string val   = m.Groups[1].Value;
            string inner = m.Groups[2].Value;
            return UrlDecodeBb(val) == capturedVal
                ? $"[url={val}][bgcolor=#1A3A6A]{inner}[/bgcolor][/url]"
                : m.Value;
        });

        int saved = _scroll?.ScrollVertical ?? 0;
        _richText.Text = prefix + highlighted;
        if (_scroll != null) _scroll.ScrollVertical = saved;
        _highlightActive = true;
    }

    // Called when the mouse cursor enters a [url=…] link
    private void OnMetaHoverStarted(Variant meta)
    {
        if (_controllerChoices.Count == 0) return; // no active menu
        string val = UrlDecodeBb(meta.AsString());
        _mouseHoverValue = val;
        int idx = _controllerChoices.IndexOf(val);
        if (idx >= 0) _controllerChoiceIndex = idx;
        UpdateHighlight();
        if (_statusBar != null)
            _statusBar.Text = $"▶  {val}  ({_controllerChoiceIndex + 1}/{_controllerChoices.Count})";
    }

    // Called when the mouse cursor leaves a [url=…] link
    private void OnMetaHoverEnded(Variant meta)
    {
        _mouseHoverValue = null;
        UpdateHighlight(); // restores plain or controller-highlight
        // Restore controller hint if controller navigation is active
        if (_controllerChoices.Count > 0 && _controllerChoiceIndex >= 0)
            _statusBar.Text = $"[コントローラー] ▶  {_controllerChoices[_controllerChoiceIndex]}  ({_controllerChoiceIndex + 1}/{_controllerChoices.Count})  ↑↓選択  Aで決定";
        else if (_statusBar != null)
            _statusBar.Text = "";
    }

    // Accept any key press for WAIT / WAITANYKEY / INPUTMOUSEKEY
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            // Function-key macro injection (F1–F12)
            if (EraConfig.UseKeyMacro && !key.AltPressed)
            {
                int fkeyIdx = GetFunctionKeyIndex(key.Keycode);
                if (fkeyIdx >= 0)
                {
                    if (key.ShiftPressed && fkeyIdx < MinorShift.Emuera.Runtime.Script.KeyMacro.MaxGroup)
                    {
                        // Shift+F1..F10 selects macro group 0..9
                        _macroGroup = fkeyIdx;
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                    if (!key.ShiftPressed)
                    {
                        string macroText = MinorShift.Emuera.Runtime.Script.KeyMacro.GetMacro(fkeyIdx, _macroGroup);
                        if (macroText != null && macroText.Length > 0 && _inputLine.Editable)
                        {
                            _inputLine.Text = macroText;
                            _inputLine.CaretColumn = macroText.Length;
                            GetViewport().SetInputAsHandled();
                            return;
                        }
                    }
                }
            }

            var req = _pendingRequest;
            var tcs = _inputTcs;
            bool hasPending = req != null && tcs != null && !tcs.Task.IsCompleted;

            // ── Escape: open settings overlay (always takes priority when settings is closed).
            //    When settings is already visible, SettingsNode handles Escape to close it.
            //    Only advance WAIT-type inputs when settings is already open (or unavailable).
            if (key.Keycode == Key.Escape)
            {
                // Open settings unconditionally when the overlay is not visible
                if (_settings != null && !_settings.Visible)
                {
                    GetViewport().SetInputAsHandled();
                    ShowSettings();
                    return;
                }
                // Settings already open — advance input only if there's a pending WAIT
                if (hasPending && (req.InputType == InputType.AnyKey || req.InputType == InputType.EnterKey))
                {
                    GetViewport().SetInputAsHandled();
                    _inputLine.Editable = false;
                    _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
                    ClearControllerState();
                    tcs.TrySetResult(new InputResult { Value = "" });
                    return;
                }
                return;
            }

            if (hasPending)
            {
                if (req.InputType == InputType.AnyKey)
                {
                    GetViewport().SetInputAsHandled();
                    _inputLine.Editable = false;
                    _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
                    tcs.TrySetResult(new InputResult { Value = key.Keycode.ToString() });
                }
                else if (req.InputType == InputType.PrimitiveMouseKey)
                {
                    // type=4: keyboard key press during INPUTMOUSEKEY
                    GetViewport().SetInputAsHandled();
                    _inputLine.Editable = false;
                    tcs.TrySetResult(new InputResult
                    {
                        MouseType   = 4,
                        MouseButton = 0,
                        MouseX      = (int)_lastMousePos.X,
                        MouseY      = (int)_lastMousePos.Y,
                    });
                }
            }
        }
    }

    // ----------------------------------------------------------------
    // Helper: enqueue an action to run on the Godot main thread
    // ----------------------------------------------------------------

    private void Enqueue(Action a) => _uiQueue.Enqueue(a);

    /// <summary>Returns 0-based function key index for F1–F12, or -1 for other keys.</summary>
    private static int GetFunctionKeyIndex(Key keycode) => keycode switch
    {
        Key.F1  => 0,  Key.F2  => 1,  Key.F3  => 2,  Key.F4  => 3,
        Key.F5  => 4,  Key.F6  => 5,  Key.F7  => 6,  Key.F8  => 7,
        Key.F9  => 8,  Key.F10 => 9,  Key.F11 => 10, Key.F12 => 11,
        _       => -1,
    };

    // ----------------------------------------------------------------
    // IGameConsole implementation
    // ----------------------------------------------------------------

    // ---- Output ----

    public void PrintString(string text, StringStyle style)
    {
        if (string.IsNullOrEmpty(text)) return;
        string bbcode = ToBbcode(text, style);
        Enqueue(() =>
        {
            _richText.AppendText(bbcode);
            _bbcodeAccum.Append(bbcode);
            TrimBbcodeAccumIfNeeded();
        });
    }

    public void PrintNewLine()
    {
        Enqueue(() =>
        {
            _richText.AppendText("\n");
            _bbcodeAccum.Append('\n');
            TrimBbcodeAccumIfNeeded();
        });
    }

    public void PrintLine(string lineChar)
    {
        // DRAWLINE — fill the console width with the given char, computed from widget size.
        char ch = (lineChar?.Length > 0) ? lineChar[0] : '-';
        Enqueue(() =>
        {
            int px = (int)_richText.Size.X;
            float charWidth = _charWidth > 0 ? _charWidth : System.Math.Max(6f, _fontSize / 2f);
            int count = px > 0 ? System.Math.Max(20, (int)(px / charWidth)) : 60;
            string ruleBb = $"[color=#888888]{new string(ch, count)}[/color]\n";
            _richText.AppendText(ruleBb);
            _bbcodeAccum.Append(ruleBb);
            TrimBbcodeAccumIfNeeded();
        });
    }

    public void ClearLine(int count)
    {
        // Remove the last `count` lines from the accumulated BBCode buffer
        Enqueue(() =>
        {
            string full = _bbcodeAccum.ToString();
            int oldLen = full.Length;
            for (int i = 0; i < count; i++)
            {
                // Guard: LastIndexOf with startIndex requires Length >= 2.
                if (full.Length < 2) { full = ""; break; }
                int nl = full.LastIndexOf('\n', full.Length - 2);
                if (nl < 0) { full = ""; break; }
                full = full[..(nl + 1)];
            }
            _richText.Text = full;
            _bbcodeAccum.Clear();
            _bbcodeAccum.Append(full);
            _lastInputPos = System.Math.Max(0, _lastInputPos - (oldLen - full.Length));
        });
    }

    public void ClearScreen()
    {
        Enqueue(() =>
        {
            _richText.Clear();
            _bbcodeAccum.Clear();
            _lastInputPos = 0;
            _menuStartPos = -1;
            _mouseHoverValue = null;
            _highlightActive = false;
        });
    }

    public void PrintHtml(string html, bool opt = false)
    {
        // Extract all absolutely positioned <div rect=...> blocks.
        // Image-only divs → CbgSet; divs with inner text/buttons → PrintHtmlDiv.
        var divImages = new List<DivImageInstruction>();
        var divTexts  = new List<DivHtmlInstruction>();
        string remainingHtml = ExtractDivInstructions(html, divImages, divTexts);

        foreach (var inst in divImages)
        {
            string path = ResolveImagePathForBbcode(inst.Source);
            CbgSet(path, inst.X, inst.Y, inst.Width, inst.Height);
        }
        foreach (var inst in divTexts)
        {
            PrintHtmlDiv(inst.InnerHtml, inst.X, inst.Y, inst.Width, inst.Height,
                         inst.Depth, inst.BColor, inst.BorderPx, inst.PaddingPx);
        }

        // Inline <img> in HTML_PRINT is rendered directly via AddImage instead of BBCode [img].
        // RichTextLabel's BBCode image loader is unreliable with non-res:// file paths.
        // Render pipeline: HtmlToOps -> DrawOps.
        var ops = ParseInlineHtmlRenderOps(remainingHtml);
        foreach (var op in ops)
        {
            if (op.Type == HtmlRenderOpType.Text)
            {
                if (string.IsNullOrEmpty(op.Text))
                    continue;

                string chunk = op.Text;
                Enqueue(() =>
                {
                    _richText.AppendText(chunk);
                    _bbcodeAccum.Append(chunk);
                    TrimBbcodeAccumIfNeeded();
                });
                continue;
            }

            if (op.Type == HtmlRenderOpType.InlineImage)
            {
                string src = op.Source;
                int width = op.Width;
                int height = op.Height;
                string resolved = ResolveImagePathForBbcode(src);
                Enqueue(() =>
                {
                    Texture2D tex = LoadTexture(resolved);
                    if (tex == null)
                    {
                        GD.PrintErr($"[HTML IMG] Image not found: {src} -> {resolved}");
                        return;
                    }

                    int drawW = width > 0 ? width : tex.GetWidth();
                    int drawH = height > 0 ? height : tex.GetHeight();
                    _richText.AddImage(tex, drawW, drawH);
                    // Keep the accumulator in sync length-wise for later linkify/highlight logic.
                    _bbcodeAccum.Append(' ');
                    TrimBbcodeAccumIfNeeded();
                });
            }
        }

        // opt=false means standalone HTML block (not inline): add trailing newline to finalize line.
        if (!opt)
        {
            Enqueue(() =>
            {
                _richText.AppendText("\n");
                _bbcodeAccum.Append('\n');
                TrimBbcodeAccumIfNeeded();
            });
        }
    }

    public void PrintHtmlDiv(string innerHtml, int x, int y, int width, int height,
                              int depth, string bcolor, int borderPx, int paddingPx)
    {
        // Capture for closure.
        string capturedHtml = innerHtml;
        int cx = x, cy = y, cw = width, ch = height, cdepth = depth, cborder = borderPx, cpad = paddingPx;
        string cbcolor = bcolor;

        Enqueue(() =>
        {
            // Outer container positioned within CbgContainer.
            var panel = new Panel();
            panel.Position = new Vector2(cx, cy);
            if (cw > 0 && ch > 0)
                panel.Size = new Vector2(cw, ch);
            panel.ClipContents = true;

            // Apply border/background styling.
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0, 0, 0, 0); // transparent by default
            if (cborder > 0 && !string.IsNullOrEmpty(cbcolor))
            {
                styleBox.BorderWidthTop    = cborder;
                styleBox.BorderWidthBottom = cborder;
                styleBox.BorderWidthLeft   = cborder;
                styleBox.BorderWidthRight  = cborder;
                styleBox.BorderColor = ParseHtmlColor(cbcolor);
            }
            panel.AddThemeStyleboxOverride("panel", styleBox);

            // Z-index approximation via z_index.
            if (cdepth != 0)
                panel.ZIndex = cdepth;

            // Inner RichTextLabel for the div content.
            var rt = new RichTextLabel();
            rt.BbcodeEnabled = true;
            rt.ScrollActive  = false;
            rt.FitContent    = true;
            rt.AnchorsPreset = (int)LayoutPreset.FullRect;
            if (cpad > 0)
            {
                rt.OffsetLeft   = cpad;
                rt.OffsetTop    = cpad;
                rt.OffsetRight  = -cpad;
                rt.OffsetBottom = -cpad;
            }

            // Copy font/size from the main RichTextLabel if available.
            if (_activeFont != null)
                rt.AddThemeFontOverride("normal_font", _activeFont);
            rt.AddThemeFontSizeOverride("normal_font_size", _fontSize);
            rt.AddThemeColorOverride("default_color", GodotColorFromArgb(_fgArgb));

            // Connect MetaClicked so buttons inside the div fire input.
            rt.MetaClicked += OnMetaClicked;

            panel.AddChild(rt);
            _cbgContainer.AddChild(panel);

            // Now render inner HTML into the sub-RichTextLabel.
            // We temporarily hijack _richText to reuse the render helpers, then restore it.
            // But that's not thread-safe. Instead, render directly.
            RenderHtmlIntoRichText(capturedHtml, rt);
        });
    }

    /// <summary>Render HTML content into an arbitrary RichTextLabel node using the same pipeline as PrintHtml.</summary>
    private void RenderHtmlIntoRichText(string html, RichTextLabel target)
    {
        // Run the same ops pipeline but output into `target` instead of `_richText`.
        var ops = ParseInlineHtmlRenderOps(html);
        foreach (var op in ops)
        {
            if (op.Type == HtmlRenderOpType.Text)
            {
                if (!string.IsNullOrEmpty(op.Text))
                    target.AppendText(op.Text);
            }
            else if (op.Type == HtmlRenderOpType.InlineImage)
            {
                string resolved = ResolveImagePathForBbcode(op.Source);
                Texture2D tex = LoadTexture(resolved);
                if (tex != null)
                {
                    int dw = op.Width  > 0 ? op.Width  : tex.GetWidth();
                    int dh = op.Height > 0 ? op.Height : tex.GetHeight();
                    target.AddImage(tex, dw, dh);
                }
                else
                {
                    GD.PrintErr($"[HTML DIV IMG] Image not found: {op.Source} -> {resolved}");
                }
            }
        }
    }

    private static Color ParseHtmlColor(string colorStr)
    {
        if (string.IsNullOrWhiteSpace(colorStr))
            return Colors.White;
        string s = colorStr.Trim();
        if (!s.StartsWith("#", StringComparison.Ordinal))
            s = "#" + s;
        try { return Color.FromString(s, Colors.White); } catch { return Colors.White; }
    }

    public void PrintImage(string resourcePath, int width, int height, int align)
    {
        // Use AddImage directly (same as HTML_PRINT inline images) to avoid BBCode [img]
        // unreliability with non-res:// paths (especially on Web/Android).
        // Alignment: 0=left, 1=center, 2=right
        string resolved = ResolveImagePathForBbcode(resourcePath);
        int w = width, h = height, a = align;
        Enqueue(() =>
        {
            Texture2D tex = LoadTexture(resolved);
            if (tex == null)
            {
                GD.PrintErr($"[PRINT_IMG] Image not found: {resourcePath} -> {resolved}");
                return;
            }
            int drawW = w > 0 ? w : tex.GetWidth();
            int drawH = h > 0 ? h : tex.GetHeight();

            string alignTag = a switch { 1 => "center", 2 => "right", _ => null };
            if (alignTag != null) _richText.AppendText($"[{alignTag}]");
            _richText.AddImage(tex, drawW, drawH);
            if (alignTag != null) _richText.AppendText($"[/{alignTag}]");
            _richText.AppendText("\n");

            // Keep accumulator in sync (placeholder char for image, then newline)
            _bbcodeAccum.Append(' ');
            _bbcodeAccum.Append('\n');
            TrimBbcodeAccumIfNeeded();
        });
    }

    public void PrintButton(string displayText, string inputValue, StringStyle style)
    {
        // Render as a URL anchor; clicking fires input.
        // Alignment must wrap OUTSIDE the [url] tag for correct Godot BBCode rendering.
        int align = style?.Align ?? 0;
        string alignTag = align switch { 1 => "center", 2 => "right", _ => null };

        // Build style without alignment for the inner text (alignment handled by outer tag).
        StringStyle innerStyle = style?.Clone();
        if (innerStyle != null) innerStyle.Align = 0;

        string urlBb = $"[url={UrlEncodeBb(inputValue)}]{ToBbcode(displayText, innerStyle)}[/url]";
        string bb = alignTag != null ? $"[{alignTag}]{urlBb}[/{alignTag}]" : urlBb;
        Enqueue(() =>
        {
            _richText.AppendText(bb);
            _bbcodeAccum.Append(bb);
            TrimBbcodeAccumIfNeeded();
        });
    }

    public void SetForeColor(int argb) => _fgArgb = argb;
    public void SetBackColor(int argb) => _bgArgb = argb;
    public void ResetColors()
    {
        _fgArgb = unchecked((int)0xFFC0C0C0);
        _bgArgb = unchecked((int)0xFF000000);
        _styleFlags = TextStyleFlags.Normal;
    }

    // ---- CBG ----

    public void CbgSet(string resourcePath, int x, int y, int width, int height)
    {
        Enqueue(() =>
        {
            if (string.IsNullOrEmpty(resourcePath)) return;
            var texture = LoadTexture(resourcePath);
            if (texture == null)
            {
                GD.PrintErr($"[CBG] Image not found: {resourcePath}");
                return;
            }
            var rect = new TextureRect();
            rect.Texture = texture;
            rect.StretchMode = TextureRect.StretchModeEnum.Scale;
            rect.Position = new Vector2(x, y);
            if (width > 0 && height > 0)
                rect.Size = new Vector2(width, height);
            else
                rect.Size = new Vector2(texture.GetWidth(), texture.GetHeight());
            _cbgContainer.AddChild(rect);
        });
    }
    public void CbgClear()
    {
        Enqueue(() =>
        {
            foreach (Node child in _cbgContainer.GetChildren())
                child.QueueFree();
        });
    }

    public void SetCbgButtonMap(string resourcePath, int width, int height)
    {
        Enqueue(() =>
        {
            _cbgButtonMapImage = null;
            if (string.IsNullOrWhiteSpace(resourcePath))
                return;

            string resolved = ResolveImagePathForBbcode(resourcePath);
            var image = new Image();
            Error err = image.Load(resolved);
            if (err != Error.Ok)
            {
                GD.PrintErr($"[CBG] Button map load failed: {resourcePath} ({err})");
                return;
            }

            _cbgButtonMapImage = image;
        });
    }

    public void ClearCbgButtonMap()
    {
        Enqueue(() => { _cbgButtonMapImage = null; });
    }

    private int SampleCbgButtonMapColor(Vector2 pos)
    {
        if (_cbgButtonMapImage == null)
            return -1;

        int x = (int)pos.X;
        int y = (int)pos.Y;
        if (x < 0 || y < 0 || x >= _cbgButtonMapImage.GetWidth() || y >= _cbgButtonMapImage.GetHeight())
            return -1;

        Color c = _cbgButtonMapImage.GetPixel(x, y);
        if (c.A < 0.999f)
            return -1;

        int r = Mathf.Clamp((int)Mathf.Round(c.R * 255f), 0, 255);
        int g = Mathf.Clamp((int)Mathf.Round(c.G * 255f), 0, 255);
        int b = Mathf.Clamp((int)Mathf.Round(c.B * 255f), 0, 255);
        return (r << 16) | (g << 8) | b;
    }

    // ---- Sound ----

    public void PlayBgm(string resourcePath)
    {
        Enqueue(() =>
        {
            if (_currentBgmPath == resourcePath && _bgmPlayer.Playing) return;
            _currentBgmPath = resourcePath;
            var stream = LoadAudioStream(resourcePath);
            if (stream == null)
            {
                GD.PrintErr($"[Audio] BGM not found: {resourcePath}");
                _bgmPlayingState = false;
                return;
            }
            _bgmPlayer.Stream = stream;
            _bgmPlayer.Play();
            _bgmPlayingState = true;
        });
    }
    public void StopBgm()
    {
        Enqueue(() =>
        {
            _bgmPlayer.Stop();
            _currentBgmPath = null;
            _bgmPlayingState = false;
        });
    }
    public void FadeBgm(int durationMs)
    {
        Enqueue(() =>
        {
            if (!_bgmPlayer.Playing) return;
            int ms = durationMs > 0 ? durationMs : 500;
            float startDb   = _bgmPlayer.VolumeDb;
            var tween = CreateTween();
            tween.TweenProperty(_bgmPlayer, "volume_db", -80f, ms / 1000.0);
            tween.TweenCallback(Callable.From(() =>
            {
                _bgmPlayer.Stop();
                _bgmPlayer.VolumeDb = startDb; // restore so next PLAYBGM is at normal volume
                _currentBgmPath = null;
                _bgmPlayingState = false;
            }));
        });
    }
    public void PlaySound(string resourcePath)
    {
        Enqueue(() =>
        {
            var stream = LoadAudioStream(resourcePath);
            if (stream == null)
            {
                GD.PrintErr($"[Audio] SE not found: {resourcePath}");
                _sePlayingState = false;
                return;
            }
            _sePlayer.Stream = stream;
            _sePlayer.Play();
            _sePlayingState = true;
        });
    }
    public void SetBgmVolume(int volume)
    {
        Enqueue(() => _bgmPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(volume / 100f, 0f, 1f)));
    }
    public void SetSeVolume(int volume)
    {
        Enqueue(() => _sePlayer.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(volume / 100f, 0f, 1f)));
    }

    public bool IsBgmPlaying() => _bgmPlayingState;

    public bool IsSePlaying() => _sePlayingState;

    // ---- User-controlled volume (persisted via SettingsNode / gemuera_settings.cfg) ----

    /// <summary>Apply a user-specified BGM volume override (0–100).</summary>
    public void SetUserBgmVolume(int volume)
    {
        Enqueue(() => _bgmPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(volume / 100f, 0f, 1f)));
    }

    /// <summary>Apply a user-specified SE volume override (0–100).</summary>
    public void SetUserSeVolume(int volume)
    {
        Enqueue(() => _sePlayer.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(volume / 100f, 0f, 1f)));
    }

    // ---- Settings screen ----

    private SettingsNode _settings;

    /// <summary>Attach the SettingsNode child (called from _Ready or wired in the scene).</summary>
    public void SetSettingsNode(SettingsNode settings)
    {
        _settings = settings;
        _settings.SettingsClosed += OnSettingsClosed;
    }

    /// <summary>Open the settings overlay (called on ESC when no input is pending).</summary>
    public void ShowSettings()
    {
        if (_settings == null) return;
        _settings.ShowSettings(this);
    }

    private void OnSettingsClosed()
    {
        // Re-apply font settings now that config was updated
        ApplyConfigFont();
    }

    /// <summary>
    /// Load an audio stream from either a Godot resource path (res://) or
    /// an absolute/relative file system path (for user-supplied game data).
    /// Supports ogg, mp3, wav.
    /// </summary>
    private static AudioStream LoadAudioStream(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // Godot resource path
        if (path.StartsWith("res://"))
            return GD.Load<AudioStream>(path);

        // Absolute / relative file-system path
        if (!System.IO.File.Exists(path)) return null;
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        byte[] data = System.IO.File.ReadAllBytes(path);
        return ext switch
        {
            ".ogg" => LoadOgg(data),
            ".mp3" => LoadMp3(data),
            ".wav" => LoadWav(data),
            _ => null,
        };
    }

    private static AudioStreamOggVorbis LoadOgg(byte[] data)
    {
        var stream = new AudioStreamOggVorbis();
        stream.PacketSequence = AudioStreamOggVorbis.LoadFromBuffer(data).PacketSequence;
        return stream;
    }

    private static AudioStreamMP3 LoadMp3(byte[] data)
    {
        var stream = new AudioStreamMP3();
        stream.Data = data;
        return stream;
    }

    private static AudioStreamWav LoadWav(byte[] data)
    {
        // Use GD.Load via temp file is complex; use ResourceLoader or parse manually.
        // For simplicity, write to user:// temp and load.
        string tempPath = "user://~temp_audio.wav";
        using (var f = Godot.FileAccess.Open(tempPath, Godot.FileAccess.ModeFlags.Write))
        {
            if (f == null) return null;
            f.StoreBuffer(data);
        }
        return GD.Load<AudioStreamWav>(tempPath);
    }

    /// <summary>Load a texture from a res:// path or a file-system path (png/jpg/bmp/webp).</summary>
    private static Texture2D LoadTexture(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // Cache hit (WeakReference — texture may have been collected if no longer held)
        if (_textureCache.TryGetValue(path, out var wr) && wr.TryGetTarget(out var cached))
            return cached;

        Texture2D tex = LoadTextureInternal(path);
        if (tex != null)
            _textureCache[path] = new WeakReference<Texture2D>(tex);
        return tex;
    }

    private static Texture2D LoadTextureInternal(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // res:// Godot resource
        if (path.StartsWith("res://"))
            return GD.Load<Texture2D>(path);

        // Actual file on disk
        if (System.IO.File.Exists(path))
        {
            var img = new Image();
            var err = img.Load(path);
            if (err != Error.Ok)
            {
                GD.PrintErr($"[LoadTexture] Image.Load failed ({err}): {path}");
                return null;
            }
            return ImageTexture.CreateFromImage(img);
        }

        // AppContents sprite fallback (for GCREATE/SPRITESETG sprites like IMG_LINE_10001)
        return TryLoadSpriteTexture(path);
    }

    private static Texture2D TryLoadSpriteTexture(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var sprite = MinorShift.Emuera.UI.Game.Image.AppContents.GetSprite(name);
        if (sprite == null) return null;

        // CroppedImage backed by a GraphicsImage  → crop from the surface
        if (sprite is MinorShift.Emuera.UI.Game.Image.CroppedImage ci)
        {
            // Resource-backed sprite (loaded from file)
            if (!string.IsNullOrEmpty(ci.ResourcePath) && System.IO.File.Exists(ci.ResourcePath))
                return LoadTexture(ci.ResourcePath);  // recursive, will be cached by path

            // GraphicsImage-backed sprite — crop region from the Godot.Image surface
            if (ci.SourceGraphics?.GodotSurface != null)
            {
                var src = ci.SourceGraphics.GodotSurface;
                var rect = ci.SourceRect;
                Godot.Rect2I safeRect = new(
                    Mathf.Clamp(rect.X, 0, src.GetWidth()),
                    Mathf.Clamp(rect.Y, 0, src.GetHeight()),
                    Mathf.Clamp(rect.Width, 0, src.GetWidth() - rect.X),
                    Mathf.Clamp(rect.Height, 0, src.GetHeight() - rect.Y));
                if (safeRect.Size.X <= 0 || safeRect.Size.Y <= 0) return null;
                var cropped = src.GetRegion(safeRect);
                return ImageTexture.CreateFromImage(cropped);
            }
        }

        return null;
    }

    private static string ResolveImagePathForBbcode(string src)
    {
        if (string.IsNullOrWhiteSpace(src))
            return src;

        if (src.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || src.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
            return src;

        string normalized = src.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar).Trim();
        if (_resolvedImagePathCache.TryGetValue(normalized, out string cached))
            return cached;

        if (Path.IsPathRooted(normalized) && File.Exists(normalized))
            return _resolvedImagePathCache[normalized] = normalized;

        string withExt = Path.HasExtension(normalized) ? normalized : null;
        string[] roots =
        {
            Program.ContentDir,
            Program.ExeDir,
        };

        foreach (string root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            if (withExt != null)
            {
                string candidate = Path.Combine(root, withExt);
                if (File.Exists(candidate))
                    return _resolvedImagePathCache[normalized] = candidate;
            }
            else
            {
                string directNoExt = Path.Combine(root, normalized);
                foreach (string ext in _imageExtCandidates)
                {
                    string candidate = directNoExt + ext;
                    if (File.Exists(candidate))
                        return _resolvedImagePathCache[normalized] = candidate;
                }
            }
        }

        if (!Path.HasExtension(normalized))
        {
            string fileStem = Path.GetFileName(normalized);
            if (!string.IsNullOrEmpty(fileStem) && !string.IsNullOrWhiteSpace(Program.ContentDir)
                && Directory.Exists(Program.ContentDir))
            {
                try
                {
                    foreach (string ext in _imageExtCandidates)
                    {
                        string pattern = fileStem + ext;
                        string[] hits = Directory.GetFiles(Program.ContentDir, pattern, SearchOption.AllDirectories);
                        if (hits.Length > 0)
                            return _resolvedImagePathCache[normalized] = hits[0];
                    }
                }
                catch
                {
                    // Fall through to the original source when recursive search is unavailable.
                }
            }
        }

        return _resolvedImagePathCache[normalized] = src;
    }

    // ---- Input ----

    public Task<InputResult> RequestInputAsync(InputRequest request)
    {
        _pendingRequest = request;
        _inputTcs = new TaskCompletionSource<InputResult>();

        Enqueue(() =>
        {
            // Auto-linkify [N] patterns (from PRINT "[N] text" + INPUT style menus)
            // for integer-based input types. Only the output since the last INPUT is scanned.
            if (request.InputType == InputType.IntValue
                || request.InputType == InputType.IntButton
                || request.InputType == InputType.AnyValue)
            {
                // NOTE: _richText.Text is NOT updated by AppendText(), so we use _bbcodeAccum.
                CompactBbcodeForRichTextReset();
                string fullBb = _bbcodeAccum.ToString();
                GD.Print($"[ConsoleNode] Linkify: accum.Length={fullBb.Length}, lastPos={_lastInputPos}");
                if (_lastInputPos < fullBb.Length)
                {
                    string newSection = fullBb[_lastInputPos..];
                    bool tooLarge = fullBb.Length > MaxLinkifyTotalChars || newSection.Length > MaxLinkifySectionChars;
                    if (tooLarge)
                    {
                        GD.Print("[ConsoleNode] Linkify skipped: section too large");
                    }
                    else
                    {
                        string converted = ConvertNumberPatterns(newSection);
                        if (converted != newSection)
                        {
                            string finalBb = string.Concat(fullBb.AsSpan(0, _lastInputPos), converted);
                            _richText.Text = finalBb;
                            _bbcodeAccum.Clear();
                            _bbcodeAccum.Append(finalBb);
                            GD.Print($"[ConsoleNode] Linkify applied, new length={finalBb.Length}");
                        }
                    }
                }
            }
            int prevLastInputPos = _lastInputPos;
            _lastInputPos = _bbcodeAccum.Length;

            // Populate controller choices from the just-linkified menu section.
            if (request.InputType == InputType.IntValue
             || request.InputType == InputType.IntButton
             || request.InputType == InputType.StrValue
             || request.InputType == InputType.StrButton
             || request.InputType == InputType.AnyValue)
            {
                string bb = _bbcodeAccum.ToString();
                // The menu is the section between old lastInputPos and new lastInputPos.
                string menuSection = prevLastInputPos < bb.Length ? bb[prevLastInputPos..] : "";
                _menuStartPos = prevLastInputPos; // store for highlight overlay
                ExtractControllerChoices(menuSection);
            }
            else
            {
                ClearControllerState();
            }

            _inputLine.Clear();

            switch (request.InputType)
            {
                case InputType.IntValue:
                case InputType.StrValue:
                case InputType.AnyValue:
                case InputType.IntButton:
                case InputType.StrButton:
                    // Normal input: enable the text box
                    _inputLine.PlaceholderText = request.InputType == InputType.StrValue
                        ? "文字列を入力 / Enter string"
                        : "数字を入力 / Enter number";
                    _inputLine.Editable = true;
                    _inputLine.GrabFocus();
                    break;

                case InputType.EnterKey:
                    // Enter key or mouse click continues
                    _inputLine.PlaceholderText = "クリックかEnterキーで続ける / Click or press Enter";
                    _inputLine.Editable = true;
                    _inputLine.GrabFocus();
                    break;

                case InputType.AnyKey:
                    // Any key, click, or Enter continues
                    _inputLine.PlaceholderText = "クリックか何かキーを押してください / Click or press any key";
                    _inputLine.Editable = true;
                    _inputLine.GrabFocus();
                    break;

                case InputType.PrimitiveMouseKey:
                    // INPUTMOUSEKEY — wait for next mouse click, scroll, or key press.
                    // The text box is NOT used; input is captured in _Input / _UnhandledKeyInput.
                    _inputLine.PlaceholderText = "クリックまたはキー入力 / Click or press any key";
                    _inputLine.Editable = false;
                    break;

                case InputType.Void:
                    // Cannot accept input — we still need to unblock eventually.
                    // Complete immediately (no variable will be set).
                    _inputTcs?.TrySetResult(new InputResult { Value = "" });
                    return;
            }

            // Preset default value if available
            if (request.HasDefValue)
            {
                _inputLine.Text = request.InputType == InputType.StrValue
                    ? (request.DefStrValue ?? "")
                    : request.DefIntValue.ToString();
            }

            // Timeout support (TINPUT / TINPUTS)
            if (request.Timelimit > 0)
            {
                var tcs = _inputTcs;
                var req = request;
                var timer = new System.Threading.Timer(_ =>
                {
                    if (tcs != null && !tcs.Task.IsCompleted)
                    {
                        Enqueue(() =>
                        {
                            _inputLine.Editable = false;
                            tcs.TrySetResult(new InputResult
                            {
                                Value = req.InputType == InputType.StrValue
                                    ? (req.DefStrValue ?? "")
                                    : req.DefIntValue.ToString(),
                                IsTimeout = true
                            });
                        });
                    }
                }, null, (int)request.Timelimit, Timeout.Infinite);
            }
        });

        return _inputTcs.Task;
    }

    // Called when the user presses Enter in the input field
    private void OnInputSubmitted(string text)
    {
        var req = _pendingRequest;
        var tcs  = _inputTcs;
        if (tcs == null || tcs.Task.IsCompleted) return;

        _inputLine.Editable = false;
        _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
        ClearControllerState();

        // For ONEINPUT: accept only 1 digit / character
        string value = text;
        if (req != null && req.OneInput && value.Length > 1)
            value = value[..1];

        tcs.TrySetResult(new InputResult { Value = value });
    }

    // Handle button (URL) clicks in RichTextLabel
    private void OnMetaClicked(Variant meta)
    {
        string val = UrlDecodeBb(meta.AsString());
        GD.Print($"[ConsoleNode] MetaClicked: meta='{val}', pending={_pendingRequest?.InputType}, tcsNull={_inputTcs == null}, completed={_inputTcs?.Task.IsCompleted}");
        var tcs = _inputTcs;
        if (tcs == null || tcs.Task.IsCompleted) return;
        _inputLine.Editable = false;
        _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
        ClearControllerState();
        tcs.TrySetResult(new InputResult
        {
            Value = val,
            IsButton = true
        });
    }

    // ---- Title / Status ----

    public void SetWindowTitle(string title)
    {
        _windowTitle = title;
        Enqueue(() => DisplayServer.WindowSetTitle(title));
    }

    private string _windowTitle = "Gemuera";
    public string GetWindowTitle() => _windowTitle;

    public void SetStatusBar(string text)
    {
        Enqueue(() => { if (_statusBar != null) _statusBar.Text = text; });
    }

    // ---- State ----

    private bool _isRunning = true;
    public bool IsRunning => _isRunning;

    public (int X, int Y) GetMousePosition()
    {
        // Called from interpreter thread; GetViewport().GetMousePosition() is main-thread only.
        // We capture the mouse position each frame and serve it here.
        return ((int)_lastMousePos.X, (int)_lastMousePos.Y);
    }

    private Vector2 _lastMousePos;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mm)
        {
            _lastMousePos = mm.Position;
            return;
        }

        // ── Intercept arrow / page keys BEFORE LineEdit steals them ─────────
        if (@event is InputEventKey navKey && navKey.Pressed && !navKey.Echo)
        {
            if (navKey.Keycode == Key.Up || navKey.Keycode == Key.Down)
            {
                bool up = navKey.Keycode == Key.Up;
                if (_controllerChoices.Count > 0)
                {
                    GetViewport().SetInputAsHandled();
                    if (up)
                        _controllerChoiceIndex = (_controllerChoiceIndex <= 0
                            ? _controllerChoices.Count : _controllerChoiceIndex) - 1;
                    else
                        _controllerChoiceIndex = (_controllerChoiceIndex + 1) % _controllerChoices.Count;
                    ApplyControllerChoice();
                    return;
                }
                else if (_scroll != null)
                {
                    GetViewport().SetInputAsHandled();
                    _scroll.ScrollVertical = Mathf.Max(0, _scroll.ScrollVertical + (up ? -80 : 80));
                    return;
                }
            }
            else if (navKey.Keycode == Key.Pageup || navKey.Keycode == Key.Pagedown)
            {
                GetViewport().SetInputAsHandled();
                if (_scroll != null)
                {
                    int delta = (int)_scroll.Size.Y;
                    _scroll.ScrollVertical = Mathf.Max(0,
                        _scroll.ScrollVertical + (navKey.Keycode == Key.Pageup ? -delta : delta));
                }
                return;
            }
        }

        // PrimitiveMouseKey — capture mouse button press or scroll wheel
        var req = _pendingRequest;
        var tcs = _inputTcs;
        if (req?.InputType == InputType.PrimitiveMouseKey && tcs != null && !tcs.Task.IsCompleted)
        {
            if (@event is InputEventMouseButton mb && mb.Pressed)
            {
                _lastMousePos = mb.Position;
                GetViewport().SetInputAsHandled();
                _inputLine.Editable = false;

                int type, button, count;
                if (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown
                    || mb.ButtonIndex == MouseButton.WheelLeft || mb.ButtonIndex == MouseButton.WheelRight)
                {
                    // type=2: scroll wheel; button = delta (+1 up, -1 down)
                    type = 2;
                    button = (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelLeft) ? 1 : -1;
                    count = 0;
                }
                else
                {
                    // type=1: mouse button; Windows MouseButtons: Left=1, Right=2, Middle=4
                    type = 1;
                    button = mb.ButtonIndex switch
                    {
                        MouseButton.Left   => 1,
                        MouseButton.Right  => 2,
                        MouseButton.Middle => 4,
                        _                  => 0,
                    };
                    count = 1; // one button pressed
                }

                int cbgColor = SampleCbgButtonMapColor(mb.Position);

                tcs.TrySetResult(new InputResult
                {
                    MouseType        = type,
                    MouseButton      = button,
                    MouseX           = (int)mb.Position.X,
                    MouseY           = (int)mb.Position.Y,
                    MouseButtonCount = cbgColor,
                    MouseExtra       = 0,
                });
            }
        }

        // EnterKey / AnyKey: any mouse button press advances (same as pressing Enter / any key)
        if (@event is InputEventMouseButton advBtn && advBtn.Pressed
            && tcs != null && !tcs.Task.IsCompleted)
        {
            if (req?.InputType == InputType.AnyKey || req?.InputType == InputType.EnterKey)
            {
                _inputLine.Editable = false;
                _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
                tcs.TrySetResult(new InputResult { Value = "" });
            }
        }
    }

    public void DoRedraw()
    {
        Enqueue(() => QueueRedraw());
    }

    public void Quit()
    {
        _isRunning = false;
        Enqueue(() => GetTree().Quit());
    }

    public void FatalError(string message)
    {
        Enqueue(() =>
        {
            string bb = $"\n[color=#ff4444][b]FATAL ERROR:[/b] {EscapeBb(message)}[/color]\n";
            _richText.AppendText(bb);
            _bbcodeAccum.Append(bb);
            _inputLine.Editable = false;
            GD.PrintErr($"[Gemuera] FATAL: {message}");
        });
    }

    public void DebugPrint(string text)
    {
        GD.Print($"[DEBUG] {text}");
    }

    // ----------------------------------------------------------------
    // BBCode helpers
    // ----------------------------------------------------------------

    // ----------------------------------------------------------------
    // Font configuration (called from MainNode after config is loaded)
    // ----------------------------------------------------------------

    /// <summary>
    /// Apply font face and size from the loaded ERA config to the RichTextLabel and InputLine.
    /// Must be called on the Godot main thread after <see cref="EraConfig"/> is populated.
    /// </summary>
    public void ApplyConfigFont()
    {
        int fontSize = EraConfig.FontSize;
        string fontName = EraConfig.FontName ?? "MS Gothic";
        _fontSize = fontSize;

        // Build a SystemFont that tries the configured face name then common CJK fallbacks.
        var sysFont = new SystemFont();
        sysFont.FontNames = new string[]
        {
            fontName,
            "MS Gothic",        // Windows CJK monospace ("ＭＳ ゴシック")
            "Yu Gothic",        // Windows 10+ CJK proportional
            "Noto Sans CJK JP", // Linux / Android
            "Noto Sans JP",
        };

        // Apply to the output area
        _richText.AddThemeFontOverride("normal_font",          sysFont);
        _richText.AddThemeFontOverride("bold_font",            sysFont);
        _richText.AddThemeFontOverride("italics_font",         sysFont);
        _richText.AddThemeFontOverride("bold_italics_font",    sysFont);
        _richText.AddThemeFontOverride("mono_font",            sysFont);
        _richText.AddThemeFontSizeOverride("normal_font_size",       fontSize);
        _richText.AddThemeFontSizeOverride("bold_font_size",         fontSize);
        _richText.AddThemeFontSizeOverride("italic_font_size",       fontSize);
        _richText.AddThemeFontSizeOverride("bold_italic_font_size",  fontSize);
        _richText.AddThemeFontSizeOverride("mono_font_size",         fontSize);

        // Apply to the input field
        _inputLine.AddThemeFontOverride("font", sysFont);
        _inputLine.AddThemeFontSizeOverride("font_size", fontSize);

        // Cache font reference and measure half-width character width
        _activeFont = sysFont;
        var szM = sysFont.GetStringSize("M", HorizontalAlignment.Left, -1, fontSize);
        _charWidth = szM.X > 0 ? szM.X : System.Math.Max(6f, fontSize / 2f);
    }

    private static string ToBbcode(string text, StringStyle style)
    {
        if (style == null) return EscapeBb(text);

        var sb = new StringBuilder();

        string alignTag = style.Align switch { 1 => "center", 2 => "right", _ => null };
        if (alignTag != null) sb.Append($"[{alignTag}]");

        if (style.ForeArgb != -1)
        {
            var col = GodotColorFromArgb(style.ForeArgb);
            sb.Append($"[color=#{col.ToHtml(false)}]");
        }
        if ((style.Flags & TextStyleFlags.Bold)      != 0) sb.Append("[b]");
        if ((style.Flags & TextStyleFlags.Italic)    != 0) sb.Append("[i]");
        if ((style.Flags & TextStyleFlags.Underline) != 0) sb.Append("[u]");
        if ((style.Flags & TextStyleFlags.Strike)    != 0) sb.Append("[s]");

        sb.Append(EscapeBb(text));

        if ((style.Flags & TextStyleFlags.Strike)    != 0) sb.Append("[/s]");
        if ((style.Flags & TextStyleFlags.Underline) != 0) sb.Append("[/u]");
        if ((style.Flags & TextStyleFlags.Italic)    != 0) sb.Append("[/i]");
        if ((style.Flags & TextStyleFlags.Bold)      != 0) sb.Append("[/b]");
        if (style.ForeArgb != -1) sb.Append("[/color]");
        if (alignTag != null) sb.Append($"[/{alignTag}]");

        return sb.ToString();
    }

    private static string EscapeBb(string s)
    {
        // Replace '[' with [lb] so it is not interpreted as a BBCode tag.
        // Replace leading/consecutive spaces with non-breaking spaces so that
        // RichTextLabel does not collapse them (important for ASCII art).
        s = s.Replace("[", "[lb]");
        // Protect runs of 2+ spaces and leading spaces from being collapsed.
        if (s.Contains("  ") || (s.Length > 0 && s[0] == ' '))
        {
            var sb2 = new StringBuilder(s.Length);
            bool prevSpace = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ')
                {
                    // First space in a run: keep as regular space; subsequent ones → NBSP.
                    // Also make the very first character NBSP if it is a space (prevents collapse).
                    if (prevSpace || i == 0)
                        sb2.Append('\u00a0');
                    else
                        sb2.Append(' ');
                    prevSpace = true;
                }
                else
                {
                    sb2.Append(s[i]);
                    prevSpace = false;
                }
            }
            s = sb2.ToString();
        }
        return s;
    }

    /// <summary>Encode a string for safe use as a [url=…] value (spaces → %20, % → %25).</summary>
    private static string UrlEncodeBb(string s)
    {
        // Escape BBCode brackets first, then percent-encode spaces so the
        // BBCode parser is not confused by whitespace inside the attribute value.
        return s.Replace("%", "%25").Replace("[", "%5B").Replace("]", "%5D").Replace(" ", "%20");
    }

    /// <summary>Decode a value previously encoded with UrlEncodeBb.</summary>
    private static string UrlDecodeBb(string s)
    {
        return s.Replace("%20", " ").Replace("%5B", "[").Replace("%5D", "]").Replace("%25", "%");
    }

    /// <summary>
    /// Converts [lb]N] patterns (ERA's escaped "[N]") in the given BBCode string to
    /// clickable [url=N] links. Sections already inside [url=…]…[/url] are preserved as-is.
    /// Called when an integer INPUT is requested so that plain PRINT menus become clickable.
    /// </summary>
    private static string ConvertNumberPatterns(string bbCode)
    {
        if (bbCode.Length == 0) return bbCode;
        var sb = new StringBuilder(bbCode.Length + 64);
        int lastIdx = 0;
        foreach (Match m in _urlTagRegex.Matches(bbCode))
        {
            // Convert text BEFORE this url block
            // group 1 = leading spaces, group 2 = number, group 3 = suffix inside bracket, group 4 = trailing text
            sb.Append(_bracketNumRegex.Replace(
                bbCode[lastIdx..m.Index],
                match => $"[url={match.Groups[2].Value}][lb]{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}]{match.Groups[4].Value}[/url]"));
            // Preserve existing url block unchanged
            sb.Append(m.Value);
            lastIdx = m.Index + m.Length;
        }
        // Convert any remaining text after the last url block
        sb.Append(_bracketNumRegex.Replace(
            bbCode[lastIdx..],
            match => $"[url={match.Groups[2].Value}][lb]{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}]{match.Groups[4].Value}[/url]"));
        return sb.ToString();
    }

    /// <summary>
    /// Keep the RichTextLabel BBCode buffer bounded to avoid large native allocations.
    /// Must be called on the main thread.
    /// </summary>
    private void TrimBbcodeAccumIfNeeded()
    {
        if (_bbcodeAccum.Length <= MaxBbcodeAccumChars)
            return;

        int remove = _bbcodeAccum.Length - TrimmedBbcodeChars;
        if (remove <= 0) return;

        string full = _bbcodeAccum.ToString();
        int cut = full.IndexOf('\n', remove);
        if (cut < 0) cut = remove;
        if (cut > full.Length) cut = full.Length;

        string kept = full[cut..];
        _bbcodeAccum.Clear();
        _bbcodeAccum.Append(kept);
        _richText.Text = kept;

        _lastInputPos = System.Math.Max(0, _lastInputPos - cut);
        _menuStartPos = System.Math.Max(-1, _menuStartPos - cut);
        _highlightActive = false;
    }

    /// <summary>
    /// Keep full RichTextLabel re-layouts bounded. Linkify/highlight paths call _richText.Text = ...,
    /// which becomes unstable if we feed the native parser very large BBCode buffers.
    /// Must be called on the main thread.
    /// </summary>
    private void CompactBbcodeForRichTextReset()
    {
        if (_bbcodeAccum.Length <= MaxRichTextResetChars)
            return;

        string full = _bbcodeAccum.ToString();
        int preserveStart = _menuStartPos >= 0 ? _menuStartPos : _lastInputPos;
        if (preserveStart < 0 || preserveStart > full.Length)
            preserveStart = full.Length;

        int requestedCut = full.Length - TargetRichTextResetChars;
        if (requestedCut <= 0)
            return;

        int cutBase = System.Math.Min(requestedCut, preserveStart);
        if (cutBase <= 0)
            return;

        int cut = full.IndexOf('\n', cutBase);
        if (cut < 0 || cut >= full.Length)
            cut = cutBase;

        string kept = full[cut..];
        _bbcodeAccum.Clear();
        _bbcodeAccum.Append(kept);

        _lastInputPos = System.Math.Max(0, _lastInputPos - cut);
        _menuStartPos = _menuStartPos >= 0 ? System.Math.Max(0, _menuStartPos - cut) : -1;
        _highlightActive = false;
        _mouseHoverValue = null;
    }

    private static Color GodotColorFromArgb(int argb)
    {
        byte a = (byte)((argb >> 24) & 0xFF);
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >>  8) & 0xFF);
        byte b = (byte)( argb        & 0xFF);
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    /// <summary>
    /// Convert ERA HTML subset to Godot RichTextLabel BBCode.
    /// Unknown/unsupported tags are stripped while plain text is BBCode-escaped.
    /// </summary>
    private static string HtmlToBbcode(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        var sb = new StringBuilder(html.Length + 64);
        var tagRegex = new Regex(@"<[^>]+>", RegexOptions.Singleline);
        var blockAlignStack = new Stack<string>();
        var linkStack = new Stack<bool>();
        var fontStack = new Stack<(bool color, bool size, bool face)>();
        var spanStack = new Stack<(bool color, bool size, bool face)>();
        int cursor = 0;

        foreach (Match tagMatch in tagRegex.Matches(html))
        {
            if (tagMatch.Index > cursor)
                sb.Append(EscapeBb(DecodeHtmlEntities(html[cursor..tagMatch.Index])));

            string tag = tagMatch.Value;
            string tagName = ExtractTagName(tag);
            bool isClosing = tag.Length >= 3 && tag[1] == '/';
            bool isSelfClosing = tag.EndsWith("/>", StringComparison.Ordinal);

            if (tagName.Length == 0)
            {
                cursor = tagMatch.Index + tagMatch.Length;
                continue;
            }

            if (tagName.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append('\n');
            }
            else if (tagName.Equals("img", StringComparison.OrdinalIgnoreCase))
            {
                if (!isClosing)
                    sb.Append(ConvertImgTagToBbcode(tag));
            }
            else if (tagName.Equals("b", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(isClosing ? "[/b]" : "[b]");
            }
            else if (tagName.Equals("i", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(isClosing ? "[/i]" : "[i]");
            }
            else if (tagName.Equals("u", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(isClosing ? "[/u]" : "[u]");
            }
            else if (tagName.Equals("s", StringComparison.OrdinalIgnoreCase)
                  || tagName.Equals("strike", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(isClosing ? "[/s]" : "[s]");
            }
            else if (tagName.Equals("font", StringComparison.OrdinalIgnoreCase))
            {
                if (isClosing)
                {
                    var (colorOpened, sizeOpened, faceOpened) = fontStack.Count > 0 ? fontStack.Pop() : (false, false, false);
                    if (sizeOpened)  sb.Append("[/font_size]");
                    if (colorOpened) sb.Append("[/color]");
                    if (faceOpened)  sb.Append("[/font]");
                }
                else
                {
                    bool colorOpened = TryGetFontColorBbcode(tag, out string colorTag);
                    bool sizeOpened  = TryGetFontSizeBbcode(tag, out string sizeTag);
                    bool faceOpened  = TryGetFontFaceBbcode(tag, out string faceTag);
                    if (sizeOpened)  sb.Append(sizeTag);
                    if (colorOpened) sb.Append(colorTag);
                    if (faceOpened)  sb.Append(faceTag);
                    fontStack.Push((colorOpened, sizeOpened, faceOpened));
                }
            }
            else if (tagName.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                if (isClosing)
                {
                    bool opened = linkStack.Count > 0 && linkStack.Pop();
                    if (opened)
                        sb.Append("[/url]");
                }
                else if (TryGetAnchorUrl(tag, out string href))
                {
                    sb.Append($"[url={UrlEncodeBb(href)}]");
                    linkStack.Push(true);
                }
                else
                {
                    linkStack.Push(false);
                }
            }
            else if (tagName.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                if (isClosing)
                {
                    bool opened = linkStack.Count > 0 && linkStack.Pop();
                    if (opened)
                        sb.Append("[/url]");
                }
                else if (TryGetButtonValue(tag, out string val))
                {
                    sb.Append($"[url={UrlEncodeBb(val)}]");
                    linkStack.Push(true);
                }
                else
                {
                    linkStack.Push(false);
                }
            }
            else if (tagName.Equals("nonbutton", StringComparison.OrdinalIgnoreCase))
            {
                if (isClosing)
                {
                    if (linkStack.Count > 0)
                        linkStack.Pop();
                }
                else
                {
                    linkStack.Push(false);
                }
            }
            else if (tagName.Equals("p", StringComparison.OrdinalIgnoreCase)
                  || tagName.Equals("div", StringComparison.OrdinalIgnoreCase))
            {
                if (!isClosing && TryGetParagraphAlignTag(tag, out string alignTag))
                {
                    sb.Append($"[{alignTag}]");
                    blockAlignStack.Push(alignTag);
                }
                else if (!isClosing)
                {
                    blockAlignStack.Push(string.Empty);
                }

                if (isClosing)
                {
                    if (blockAlignStack.Count > 0)
                    {
                        string closingAlignTag = blockAlignStack.Pop();
                        if (!string.IsNullOrEmpty(closingAlignTag))
                            sb.Append($"[/{closingAlignTag}]");
                    }
                    sb.Append('\n');
                }
            }
            else if (tagName.Equals("span", StringComparison.OrdinalIgnoreCase))
            {
                if (isClosing)
                {
                    var (c, s, f) = spanStack.Count > 0 ? spanStack.Pop() : (false, false, false);
                    if (s) sb.Append("[/font_size]");
                    if (c) sb.Append("[/color]");
                    if (f) sb.Append("[/font]");
                }
                else
                {
                    bool colorOpened = false, sizeOpened = false, faceOpened = false;
                    string styleAttr = ParseTagAttributes(tag).TryGetValue("style", out string sv) ? sv : null;
                    if (styleAttr != null)
                    {
                        ParseInlineCssStyle(styleAttr,
                            out string colorTag, out string sizeTag, out string faceTag);
                        if (sizeTag  != null) { sb.Append(sizeTag);  sizeOpened  = true; }
                        if (colorTag != null) { sb.Append(colorTag); colorOpened = true; }
                        if (faceTag  != null) { sb.Append(faceTag);  faceOpened  = true; }
                    }
                    spanStack.Push((colorOpened, sizeOpened, faceOpened));
                }
            }
            else if (tagName.Equals("nobr", StringComparison.OrdinalIgnoreCase))
            {
                // nobr: no line-break — BBCode has no direct equivalent; just ignore the tag
            }
            else
            {
                // Keep unknown tags stripped; content is preserved via escaped text pieces.
            }

            if (isSelfClosing && (tagName.Equals("p", StringComparison.OrdinalIgnoreCase)
                || tagName.Equals("div", StringComparison.OrdinalIgnoreCase)))
            {
                sb.Append('\n');
            }

            cursor = tagMatch.Index + tagMatch.Length;
        }

        if (cursor < html.Length)
            sb.Append(EscapeBb(DecodeHtmlEntities(html[cursor..])));

        string converted = _stripHtmlTagRegex.Replace(sb.ToString(), string.Empty);
        return converted;
    }

    private static string ExtractTagName(string tag)
    {
        if (string.IsNullOrEmpty(tag) || tag[0] != '<')
            return string.Empty;

        int i = 1;
        if (i < tag.Length && tag[i] == '/')
            i++;

        while (i < tag.Length && char.IsWhiteSpace(tag[i]))
            i++;

        int start = i;
        while (i < tag.Length && !char.IsWhiteSpace(tag[i]) && tag[i] != '>' && tag[i] != '/')
            i++;

        return i > start ? tag[start..i] : string.Empty;
    }

    private static string ConvertImgTagToBbcode(string fullTag)
    {
        Match match = _imgTagRegex.Match(fullTag);
        if (!match.Success)
            return string.Empty;

        string attrText = match.Groups[1].Value;
        string src = null;
        int width = 0;
        int height = 0;

        foreach (Match attr in _attrRegex.Matches(attrText))
        {
            string key = attr.Groups[1].Value;
            string value = GetAttrValue(attr);
            if (key.Equals("src", StringComparison.OrdinalIgnoreCase))
            {
                src = value;
            }
            else if (key.Equals("width", StringComparison.OrdinalIgnoreCase))
            {
                ParseImageDimension(value, out width);
            }
            else if (key.Equals("height", StringComparison.OrdinalIgnoreCase))
            {
                ParseImageDimension(value, out height);
            }
        }

        if (string.IsNullOrWhiteSpace(src))
            return string.Empty;

        string resolved = ResolveImagePathForBbcode(src);
        string sizeAttr = (width > 0 && height > 0) ? $" width={width} height={height}" : string.Empty;
        return $"[img{sizeAttr}]{resolved}[/img]";
    }

    private static bool TryParseInlineImgTag(string fullTag, out string src, out int width, out int height)
    {
        src = string.Empty;
        width = 0;
        height = 0;

        Match match = _imgTagRegex.Match(fullTag ?? string.Empty);
        if (!match.Success)
            return false;

        var attrs = ParseAttributes(match.Groups[1].Value);
        if (!attrs.TryGetValue("src", out src) || string.IsNullOrWhiteSpace(src))
            return false;

        if (attrs.TryGetValue("width", out string widthRaw))
            ParseImageDimension(widthRaw, out width);
        if (attrs.TryGetValue("height", out string heightRaw))
            ParseImageDimension(heightRaw, out height);

        return true;
    }

    private static bool TryGetFontSizeBbcode(string fullTag, out string sizeTag)
    {
        sizeTag = null;
        var attrs = ParseTagAttributes(fullTag);
        if (!attrs.TryGetValue("size", out string sizeRaw))
            return false;

        sizeRaw = DecodeHtmlEntities(sizeRaw).Trim();
        if (string.IsNullOrEmpty(sizeRaw))
            return false;

        // Strip "px" suffix and parse.
        string numStr = sizeRaw.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            ? sizeRaw[..^2].TrimEnd()
            : sizeRaw;
        if (int.TryParse(numStr, out int size) && size > 0)
        {
            sizeTag = $"[font_size={size}]";
            return true;
        }
        return false;
    }

    private static bool TryGetFontColorBbcode(string fullTag, out string colorTag)
    {
        colorTag = null;
        var attrs = ParseTagAttributes(fullTag);
        if (!attrs.TryGetValue("color", out string colorRaw))
            return false;

        colorRaw = DecodeHtmlEntities(colorRaw).Trim();
        if (string.IsNullOrEmpty(colorRaw))
            return false;

        if (!colorRaw.StartsWith("#", StringComparison.Ordinal))
            colorRaw = "#" + colorRaw;

        if (Regex.IsMatch(colorRaw, "^#[0-9a-fA-F]{6}$") || Regex.IsMatch(colorRaw, "^#[0-9a-fA-F]{8}$"))
        {
            colorTag = $"[color={colorRaw}]";
            return true;
        }
        return false;
    }

    private static bool TryGetFontFaceBbcode(string fullTag, out string faceTag)
    {
        faceTag = null;
        var attrs = ParseTagAttributes(fullTag);
        if (!attrs.TryGetValue("face", out string faceRaw))
            return false;

        faceRaw = DecodeHtmlEntities(faceRaw).Trim();
        if (string.IsNullOrEmpty(faceRaw))
            return false;

        // Godot BBCode font tag: [font=FontName]
        faceTag = $"[font={faceRaw}]";
        return true;
    }

    private static void ParseInlineCssStyle(string style,
        out string colorTag, out string sizeTag, out string faceTag)
    {
        colorTag = sizeTag = faceTag = null;
        if (string.IsNullOrWhiteSpace(style)) return;

        foreach (string declaration in style.Split(';'))
        {
            int colon = declaration.IndexOf(':');
            if (colon < 0) continue;
            string prop  = declaration[..colon].Trim().ToLowerInvariant();
            string value = declaration[(colon + 1)..].Trim();

            switch (prop)
            {
                case "color":
                    if (value.Length > 0)
                    {
                        string cv = value.StartsWith("#") ? value : "#" + value;
                        if (Regex.IsMatch(cv, "^#[0-9a-fA-F]{6}$") || Regex.IsMatch(cv, "^#[0-9a-fA-F]{8}$"))
                            colorTag = $"[color={cv}]";
                    }
                    break;

                case "font-size":
                    string numStr = value.EndsWith("px", StringComparison.OrdinalIgnoreCase)
                        ? value[..^2].TrimEnd() : value;
                    if (int.TryParse(numStr, out int px) && px > 0)
                        sizeTag = $"[font_size={px}]";
                    break;

                case "font-family":
                    string family = value.Split(',')[0].Trim().Trim('"', '\'');
                    if (family.Length > 0)
                        faceTag = $"[font={family}]";
                    break;
            }
        }
    }

    private static bool TryGetButtonValue(string fullTag, out string value)
    {
        value = string.Empty;
        var attrs = ParseTagAttributes(fullTag);
        if (!attrs.TryGetValue("value", out string raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        value = DecodeHtmlEntities(raw).Trim();
        return value.Length > 0;
    }

    private static bool TryGetParagraphAlignTag(string fullTag, out string alignTag)
    {
        alignTag = string.Empty;
        var attrs = ParseTagAttributes(fullTag);
        if (!attrs.TryGetValue("align", out string alignRaw) || string.IsNullOrWhiteSpace(alignRaw))
            return false;

        string v = DecodeHtmlEntities(alignRaw).Trim().ToLowerInvariant();
        alignTag = v switch
        {
            "center" => "center",
            "right" => "right",
            _ => string.Empty,
        };
        return alignTag.Length > 0;
    }

    private static bool TryGetAnchorUrl(string fullTag, out string href)
    {
        href = string.Empty;
        if (string.IsNullOrWhiteSpace(fullTag))
            return false;

        int start = fullTag.IndexOf(' ');
        int end = fullTag.LastIndexOf('>');
        if (start < 0 || end <= start)
            return false;

        var attrs = ParseAttributes(fullTag[start..end]);
        if (!attrs.TryGetValue("href", out string rawHref) || string.IsNullOrWhiteSpace(rawHref))
            return false;

        href = DecodeHtmlEntities(rawHref).Trim();
        if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private static string DecodeHtmlEntities(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return Regex.Replace(input, @"&(#x?[0-9A-Fa-f]+|[A-Za-z]+);", match =>
        {
            string token = match.Groups[1].Value;
            switch (token.ToLowerInvariant())
            {
                case "amp": return "&";
                case "lt": return "<";
                case "gt": return ">";
                case "quot": return "\"";
                case "apos": return "'";
                case "nbsp": return "\u00a0";
            }

            if (token.StartsWith("#x", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(token[2..], System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out int codeHex)
                    && codeHex >= 0 && codeHex <= 0x10FFFF)
                {
                    return char.ConvertFromUtf32(codeHex);
                }
                return match.Value;
            }

            if (token.StartsWith("#", StringComparison.Ordinal))
            {
                if (int.TryParse(token[1..], out int codeDec)
                    && codeDec >= 0 && codeDec <= 0x10FFFF)
                {
                    return char.ConvertFromUtf32(codeDec);
                }
            }

            return match.Value;
        });
    }

    private static string GetAttrValue(Match attr)
    {
        if (attr.Groups.Count < 5)
            return string.Empty;
        if (attr.Groups[2].Success)
            return attr.Groups[2].Value;
        if (attr.Groups[3].Success)
            return attr.Groups[3].Value;
        if (attr.Groups[4].Success)
            return attr.Groups[4].Value;
        return string.Empty;
    }

    private static void ParseImageDimension(string raw, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return;

        string numeric = raw.Trim();
        bool isPx = numeric.EndsWith("px", StringComparison.OrdinalIgnoreCase);
        if (isPx)
            numeric = numeric[..^2].Trim();
        if (!int.TryParse(numeric, out int parsed))
            return;

        // Emuera MixedNum behavior: unitless values are scaled by FontSize/100,
        // while explicit px values are treated as absolute pixels.
        value = isPx ? parsed : parsed * EraConfig.FontSize / 100;
    }

    private readonly struct DivImageInstruction
    {
        public DivImageInstruction(string source, int x, int y, int width, int height)
        {
            Source = source;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public string Source { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
    }

    private sealed class DivHtmlInstruction
    {
        public DivHtmlInstruction(string innerHtml, int x, int y, int width, int height,
                                   int depth, string bcolor, int borderPx, int paddingPx)
        {
            InnerHtml = innerHtml;
            X = x; Y = y; Width = width; Height = height;
            Depth = depth; BColor = bcolor; BorderPx = borderPx; PaddingPx = paddingPx;
        }
        public string InnerHtml { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public string BColor { get; }
        public int BorderPx { get; }
        public int PaddingPx { get; }
    }

    private enum HtmlRenderOpType
    {
        Text,
        InlineImage,
    }

    private readonly struct HtmlRenderOp
    {
        private HtmlRenderOp(HtmlRenderOpType type, string text, string source, int width, int height)
        {
            Type = type;
            Text = text;
            Source = source;
            Width = width;
            Height = height;
        }

        public HtmlRenderOpType Type { get; }
        public string Text { get; }
        public string Source { get; }
        public int Width { get; }
        public int Height { get; }

        public static HtmlRenderOp TextOp(string text) =>
            new(HtmlRenderOpType.Text, text ?? string.Empty, string.Empty, 0, 0);

        public static HtmlRenderOp InlineImageOp(string source, int width, int height) =>
            new(HtmlRenderOpType.InlineImage, string.Empty, source ?? string.Empty, width, height);
    }

    private static List<HtmlRenderOp> ParseInlineHtmlRenderOps(string html)
    {
        var ops = new List<HtmlRenderOp>();
        string body = html ?? string.Empty;
        int cursor = 0;

        foreach (Match img in _imgTagRegex.Matches(body))
        {
            if (img.Index > cursor)
            {
                string segment = body[cursor..img.Index];
                string converted = HtmlToBbcode(segment);
                if (!string.IsNullOrEmpty(converted))
                    ops.Add(HtmlRenderOp.TextOp(converted));
            }

            if (TryParseInlineImgTag(img.Value, out string src, out int width, out int height))
                ops.Add(HtmlRenderOp.InlineImageOp(src, width, height));

            cursor = img.Index + img.Length;
        }

        if (cursor < body.Length)
        {
            string tail = body[cursor..];
            string converted = HtmlToBbcode(tail);
            if (!string.IsNullOrEmpty(converted))
                ops.Add(HtmlRenderOp.TextOp(converted));
        }

        return ops;
    }

    /// <summary>
    /// Extract all &lt;div rect=...&gt;...&lt;/div&gt; blocks from <paramref name="html"/>.
    /// Divs whose sole inner content is a single &lt;img&gt; are added to <paramref name="images"/>.
    /// All other rect-divs (text, buttons, mixed) are added to <paramref name="htmlDivs"/>.
    /// Returns the remaining HTML with all extracted divs removed.
    /// </summary>
    private static string ExtractDivInstructions(string html,
        List<DivImageInstruction> images, List<DivHtmlInstruction> htmlDivs)
    {
        return _divTagRegex.Replace(html ?? string.Empty, match =>
        {
            var divAttrs = ParseAttributes(match.Groups[1].Value);
            if (!divAttrs.TryGetValue("rect", out string rectRaw))
                return match.Value;   // No rect → keep as inline HTML.
            if (!TryParseRect(rectRaw, out int x, out int y, out int w, out int h))
                return match.Value;

            int depth = 0;
            if (divAttrs.TryGetValue("depth", out string depthRaw))
                ParseImageDimension(depthRaw, out depth);

            string bcolor = null;
            if (divAttrs.TryGetValue("bcolor", out string bc) && !string.IsNullOrWhiteSpace(bc))
                bcolor = bc.Trim();

            int borderPx = 0;
            if (divAttrs.TryGetValue("border", out string borderRaw))
                ParseImageDimension(borderRaw, out borderPx);

            int paddingPx = 0;
            if (divAttrs.TryGetValue("padding", out string padRaw))
                ParseImageDimension(padRaw, out paddingPx);

            string innerContent = match.Groups[2].Value.Trim();

            // Check if this is purely an <img> element (CBG image overlay).
            Match imgMatch = _imgTagRegex.Match(innerContent);
            bool isImageOnly = imgMatch.Success &&
                               _imgTagRegex.Replace(innerContent, string.Empty).Trim().Length == 0;

            if (isImageOnly)
            {
                var imgAttrs = ParseAttributes(imgMatch.Groups[1].Value);
                if (imgAttrs.TryGetValue("src", out string src) && !string.IsNullOrWhiteSpace(src))
                {
                    if (imgAttrs.TryGetValue("width",  out string wr)) { ParseImageDimension(wr, out int imgW); if (imgW > 0) w = imgW; }
                    if (imgAttrs.TryGetValue("height", out string hr)) { ParseImageDimension(hr, out int imgH); if (imgH > 0) h = imgH; }
                    if (imgAttrs.TryGetValue("xpos",   out string xr)) { ParseImageDimension(xr, out int imgX); x += imgX; }
                    if (imgAttrs.TryGetValue("ypos",   out string yr)) { ParseImageDimension(yr, out int imgY); y += imgY; }

                    images.Add(new DivImageInstruction(src, x, y, w, h));
                    return string.Empty;
                }
            }

            // Non-image (or empty) div — render as positioned HTML overlay.
            htmlDivs.Add(new DivHtmlInstruction(innerContent, x, y, w, h, depth, bcolor, borderPx, paddingPx));
            return string.Empty;
        });
    }

    // Keep for backward compat (not called externally; kept for easy rollback reference).
    private static List<DivImageInstruction> ExtractDivImageInstructions(string html, out string remainingHtml)
    {
        var images = new List<DivImageInstruction>();
        var unused = new List<DivHtmlInstruction>();
        remainingHtml = ExtractDivInstructions(html, images, unused);
        return images;
    }

    private static Dictionary<string, string> ParseAttributes(string attrText)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(attrText))
            return attrs;

        foreach (Match attr in _attrRegex.Matches(attrText))
        {
            string key = attr.Groups[1].Value;
            string value = GetAttrValue(attr);
            if (!string.IsNullOrEmpty(key))
                attrs[key] = value;
        }
        return attrs;
    }

    private static Dictionary<string, string> ParseTagAttributes(string fullTag)
    {
        if (string.IsNullOrWhiteSpace(fullTag))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        int start = fullTag.IndexOf(' ');
        int end = fullTag.LastIndexOf('>');
        if (start < 0 || end <= start)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string attrText = fullTag[start..end].Trim();
        if (attrText.EndsWith("/", StringComparison.Ordinal))
            attrText = attrText[..^1].TrimEnd();
        return ParseAttributes(attrText);
    }

    private static bool TryParseRect(string rectRaw, out int x, out int y, out int width, out int height)
    {
        x = 0;
        y = 0;
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(rectRaw))
            return false;

        string[] parts = rectRaw.Split(',');
        if (parts.Length < 4)
            return false;

        ParseImageDimension(parts[0], out x);
        ParseImageDimension(parts[1], out y);
        ParseImageDimension(parts[2], out width);
        ParseImageDimension(parts[3], out height);
        return true;
    }
}
