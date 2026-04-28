// Gemuera — Console UI Node
// Implements IGameConsole using Godot RichTextLabel + LineEdit.
// Attach to Console.tscn root Control node.
using Godot;
using Gemuera.Bridge;
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

    // ----------------------------------------------------------------
    // Runtime fields
    // ----------------------------------------------------------------

    private RichTextLabel  _richText;
    private LineEdit        _inputLine;
    private Label           _statusBar;
    private ScrollContainer _scroll;

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
        _richText.ScrollFollowingEnabled = true;

        _inputLine.TextSubmitted += OnInputSubmitted;
        _inputLine.Editable = false; // disabled until INPUT command
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
        // DRAWLINE — fill the console width with the given char
        string ruleBb = $"[color=#888888]{new string((lineChar?.Length > 0 ? lineChar[0] : '-'), 60)}[/color]\n";
        Enqueue(() => _richText.AppendText(ruleBb));
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
        // TODO Phase 2: embed inline image via BBCode [img] tag
        Enqueue(() => _richText.AppendText($"[img]{resourcePath}[/img]\n"));
    }

    public void PrintButton(string displayText, string inputValue, StringStyle style)
    {
        // Render as a URL anchor; clicking fires input
        string bb = $"[url={GD.VarToStr(inputValue)}]{ToBbcode(displayText, style)}[/url]";
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
        // TODO Phase 2: implement background image via TextureRect
    }
    public void CbgClear() { /* TODO Phase 2 */ }

    // ---- Sound ----

    public void PlayBgm(string resourcePath)
    {
        Enqueue(() =>
        {
            // TODO Phase 5: load AudioStreamOggVorbis / MP3 and play
            GD.Print($"[Audio] PlayBgm: {resourcePath}");
        });
    }
    public void StopBgm()
    {
        Enqueue(() => GD.Print("[Audio] StopBgm"));
    }
    public void PlaySound(string resourcePath)
    {
        Enqueue(() => GD.Print($"[Audio] PlaySound: {resourcePath}"));
    }

    // ---- Input ----

    public Task<InputResult> RequestInputAsync(InputRequest request)
    {
        _pendingRequest = request;
        _inputTcs = new TaskCompletionSource<InputResult>();

        Enqueue(() =>
        {
            _inputLine.Clear();
            _inputLine.Editable = true;
            _inputLine.GrabFocus();

            // Handle timeout
            if (request.Timelimit > 0)
            {
                var timer = new Timer(_ =>
                {
                    var tcs = _inputTcs;
                    if (tcs != null && !tcs.Task.IsCompleted)
                    {
                        Enqueue(() =>
                        {
                            _inputLine.Editable = false;
                            tcs.TrySetResult(new InputResult
                            {
                                Value = request.DefStrValue ?? "",
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
        _inputLine.Editable = false;
        _inputTcs?.TrySetResult(new InputResult { Value = text });
    }

    // Handle button (URL) clicks in RichTextLabel
    private void OnMetaClicked(Variant meta)
    {
        string val = meta.AsString();
        _inputLine.Editable = false;
        _inputTcs?.TrySetResult(new InputResult
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

    public void SetStatusBar(string text)
    {
        Enqueue(() => { if (_statusBar != null) _statusBar.Text = text; });
    }

    // ---- State ----

    public void Redraw()
    {
        Enqueue(() => QueueRedraw());
    }

    public void Quit()
    {
        Enqueue(() => GetTree().Quit());
    }

    public void FatalError(string message)
    {
        Enqueue(() =>
        {
            _richText.AppendText($"\n[color=#ff4444][b]FATAL ERROR:[/b] {GD.StrToVar(message)}[/color]\n");
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
