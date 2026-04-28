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
using System.Text;
using System.Text.RegularExpressions;
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
    // Match [lb]N] plus anything after it up to newline, [lb], or a closing BBCode tag [/.
    // Captures group 1 = number, group 2 = trailing text on the same option.
    private static readonly Regex _bracketNumRegex =
        new(@"\[lb\](\d+)\]((?:(?!\[lb\])(?!\[/)(?!\n).)*)",
            RegexOptions.Compiled);

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
        _richText.MetaClicked += OnMetaClicked;

        _bgmPlayer = GetNode<AudioStreamPlayer>(BgmPlayerPath);
        _sePlayer  = GetNode<AudioStreamPlayer>(SePlayerPath);
        _bgmPlayer.Finished += () => { /* loop BGM */ if (_bgmPlayer.Stream != null) _bgmPlayer.Play(); };
        _cbgContainer = GetNode<Control>(CbgContainerPath);
    }

    // Drain the UI action queue every frame
    public override void _Process(double delta)
    {
        while (_uiQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception e) { GD.PrintErr($"[ConsoleNode] UI action error: {e}"); }
        }
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
            if (req != null && tcs != null && !tcs.Task.IsCompleted)
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
        Enqueue(() => { _richText.AppendText(bbcode); _bbcodeAccum.Append(bbcode); });
    }

    public void PrintNewLine()
    {
        Enqueue(() => { _richText.AppendText("\n"); _bbcodeAccum.Append('\n'); });
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
        Enqueue(() => { _richText.Clear(); _bbcodeAccum.Clear(); _lastInputPos = 0; });
    }

    public void PrintHtml(string html)
    {
        // Convert ERA HTML subset to BBCode (basic mapping)
        string bbcode = HtmlToBbcode(html) + "\n";
        Enqueue(() => { _richText.AppendText(bbcode); _bbcodeAccum.Append(bbcode); });
    }

    public void PrintImage(string resourcePath, int width, int height, int align)
    {
        // Phase 2: inline image via BBCode [img] tag with optional size.
        // resourcePath should be a Godot res:// or user:// path.
        // Alignment: 0=left, 1=center, 2=right
        string alignTag = align switch { 1 => "center", 2 => "right", _ => "left" };
        string sizeAttr = (width > 0 && height > 0) ? $" width={width} height={height}" : "";
        string bb = $"[{alignTag}][img{sizeAttr}]{resourcePath}[/img][/{alignTag}]\n";
        Enqueue(() => { _richText.AppendText(bb); _bbcodeAccum.Append(bb); });
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

        string urlBb = $"[url={EscapeBb(inputValue)}]{ToBbcode(displayText, innerStyle)}[/url]";
        string bb = alignTag != null ? $"[{alignTag}]{urlBb}[/{alignTag}]" : urlBb;
        Enqueue(() => { _richText.AppendText(bb); _bbcodeAccum.Append(bb); });
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
                return;
            }
            _bgmPlayer.Stream = stream;
            _bgmPlayer.Play();
        });
    }
    public void StopBgm()
    {
        Enqueue(() =>
        {
            _bgmPlayer.Stop();
            _currentBgmPath = null;
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
                return;
            }
            _sePlayer.Stream = stream;
            _sePlayer.Play();
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
        if (path.StartsWith("res://"))
            return GD.Load<Texture2D>(path);
        if (!System.IO.File.Exists(path)) return null;
        var img = new Image();
        var err = img.Load(path);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[CBG] Image.Load failed ({err}): {path}");
            return null;
        }
        return ImageTexture.CreateFromImage(img);
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
                string fullBb = _bbcodeAccum.ToString();
                GD.Print($"[ConsoleNode] Linkify: accum.Length={fullBb.Length}, lastPos={_lastInputPos}");
                if (_lastInputPos < fullBb.Length)
                {
                    string newSection = fullBb[_lastInputPos..];
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
            _lastInputPos = _bbcodeAccum.Length;

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

        // For ONEINPUT: accept only 1 digit / character
        string value = text;
        if (req != null && req.OneInput && value.Length > 1)
            value = value[..1];

        tcs.TrySetResult(new InputResult { Value = value });
    }

    // Handle button (URL) clicks in RichTextLabel
    private void OnMetaClicked(Variant meta)
    {
        string val = meta.AsString();
        GD.Print($"[ConsoleNode] MetaClicked: meta='{val}', pending={_pendingRequest?.InputType}, tcsNull={_inputTcs == null}, completed={_inputTcs?.Task.IsCompleted}");
        var tcs = _inputTcs;
        if (tcs == null || tcs.Task.IsCompleted) return;
        _inputLine.Editable = false;
        _inputLine.PlaceholderText = "入力してEnterキー / Type and press Enter";
        tcs.TrySetResult(new InputResult
        {
            Value = val,
            IsButton = true
        });
    }

    // ---- Title / Status ----

    public void SetWindowTitle(string title)
    {
        Enqueue(() => DisplayServer.WindowSetTitle(title));
    }

    public string GetWindowTitle() => Engine.GetVersionInfo().ToString();

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

        // PrimitiveMouseKey — capture mouse button press or scroll wheel
        var req = _pendingRequest;
        var tcs = _inputTcs;
        if (req?.InputType == InputType.PrimitiveMouseKey && tcs != null && !tcs.Task.IsCompleted)
        {
            if (@event is InputEventMouseButton mb && mb.Pressed)
            {
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

                tcs.TrySetResult(new InputResult
                {
                    MouseType        = type,
                    MouseButton      = button,
                    MouseX           = (int)mb.Position.X,
                    MouseY           = (int)mb.Position.Y,
                    MouseButtonCount = count,
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
        return s.Replace("[", "[lb]");
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
            // group 1 = number, group 2 = trailing text (the option label after the [N])
            sb.Append(_bracketNumRegex.Replace(
                bbCode[lastIdx..m.Index],
                match => $"[url={match.Groups[1].Value}][lb]{match.Groups[1].Value}]{match.Groups[2].Value}[/url]"));
            // Preserve existing url block unchanged
            sb.Append(m.Value);
            lastIdx = m.Index + m.Length;
        }
        // Convert any remaining text after the last url block
        sb.Append(_bracketNumRegex.Replace(
            bbCode[lastIdx..],
            match => $"[url={match.Groups[1].Value}][lb]{match.Groups[1].Value}]{match.Groups[2].Value}[/url]"));
        return sb.ToString();
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
    /// Very basic ERA HTML → BBCode conversion for HTML_PRINT.
    /// Full implementation belongs in Phase 2.
    /// </summary>
    private static string HtmlToBbcode(string html)
    {
        return html
            .Replace("<br>",   "\n").Replace("<BR>", "\n")
            .Replace("<b>",    "[b]").Replace("</b>", "[/b]")
            .Replace("<i>",    "[i]").Replace("</i>", "[/i]")
            .Replace("<u>",    "[u]").Replace("</u>", "[/u]")
            .Replace("<s>",    "[s]").Replace("</s>", "[/s]")
            // Remove unsupported tags
            .Replace("<nobr>", "").Replace("</nobr>", "");
    }
}
