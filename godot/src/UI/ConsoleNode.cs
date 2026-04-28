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

    // Current input request
    private TaskCompletionSource<InputResult> _inputTcs;
    private InputRequest _pendingRequest;

    // Output queue (interpreter thread → UI thread)
    private readonly ConcurrentQueue<Action> _uiQueue = new();

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
        // ScrollFollowingEnabled is not available in Godot 4.3; scroll following is enabled by default

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

    // Accept any key press for WAIT / WAITANYKEY
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
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
            }
        }
    }

    // ----------------------------------------------------------------
    // Helper: enqueue an action to run on the Godot main thread
    // ----------------------------------------------------------------

    private void Enqueue(Action a) => _uiQueue.Enqueue(a);

    // ----------------------------------------------------------------
    // IGameConsole implementation
    // ----------------------------------------------------------------

    // ---- Output ----

    public void PrintString(string text, StringStyle style)
    {
        if (string.IsNullOrEmpty(text)) return;
        string bbcode = ToBbcode(text, style);
        Enqueue(() => _richText.AppendText(bbcode));
    }

    public void PrintNewLine()
    {
        Enqueue(() => _richText.AppendText("\n"));
    }

    public void PrintLine(string lineChar)
    {
        // DRAWLINE — fill the console width with the given char, computed from widget size.
        char ch = (lineChar?.Length > 0) ? lineChar[0] : '-';
        Enqueue(() =>
        {
            // Approximate character count from pixel width (assume ~10px per char at 18px font)
            int px = (int)_richText.Size.X;
            int count = px > 0 ? System.Math.Max(20, px / 10) : 60;
            string ruleBb = $"[color=#888888]{new string(ch, count)}[/color]\n";
            _richText.AppendText(ruleBb);
        });
    }

    public void ClearLine(int count)
    {
        // Remove the last `count` lines from the RichTextLabel text
        Enqueue(() =>
        {
            string full = _richText.Text;
            for (int i = 0; i < count; i++)
            {
                int nl = full.LastIndexOf('\n', full.Length - 2);
                if (nl < 0) { full = ""; break; }
                full = full[..(nl + 1)];
            }
            _richText.Text = full;
        });
    }

    public void ClearScreen()
    {
        Enqueue(() => _richText.Clear());
    }

    public void PrintHtml(string html)
    {
        // Convert ERA HTML subset to BBCode (basic mapping)
        string bbcode = HtmlToBbcode(html);
        Enqueue(() => _richText.AppendText(bbcode + "\n"));
    }

    public void PrintImage(string resourcePath, int width, int height, int align)
    {
        // Phase 2: inline image via BBCode [img] tag with optional size.
        // resourcePath should be a Godot res:// or user:// path.
        // Alignment: 0=left, 1=center, 2=right
        string alignTag = align switch { 1 => "center", 2 => "right", _ => "left" };
        string sizeAttr = (width > 0 && height > 0) ? $" width={width} height={height}" : "";
        string bb = $"[{alignTag}][img{sizeAttr}]{resourcePath}[/img][/{alignTag}]\n";
        Enqueue(() => _richText.AppendText(bb));
    }

    public void PrintButton(string displayText, string inputValue, StringStyle style)
    {
        // Render as a URL anchor; clicking fires input.
        // inputValue is used verbatim as the url attribute so OnMetaClicked receives it unmodified.
        string bb = $"[url={EscapeBb(inputValue)}]{ToBbcode(displayText, style)}[/url]";
        Enqueue(() => _richText.AppendText(bb));
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
                    // Just press Enter to continue
                    _inputLine.PlaceholderText = "Enterキーで続ける / Press Enter";
                    _inputLine.Editable = true;
                    _inputLine.GrabFocus();
                    break;

                case InputType.AnyKey:
                    // Any key or Enter continues
                    _inputLine.PlaceholderText = "何かキーを押してください / Press any key";
                    _inputLine.Editable = true;
                    _inputLine.GrabFocus();
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
        var tcs = _inputTcs;
        if (tcs == null || tcs.Task.IsCompleted) return;

        string val = meta.AsString();
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
            _lastMousePos = mm.Position;
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
            _richText.AppendText($"\n[color=#ff4444][b]FATAL ERROR:[/b] {EscapeBb(message)}[/color]\n");
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
