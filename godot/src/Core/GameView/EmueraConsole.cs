// EmueraConsole adapter for Gemuera.
// Wraps IGameConsole (Godot UI layer) and exposes the full ERA console API
// that the interpreter core (Process, ExpressionMediator, etc.) calls.
// Lives in MinorShift.Emuera.GameView to match the original Emuera namespace.

using Gemuera.Bridge;
using MinorShift.Emuera.Runtime;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.UI.Game;
using MinorShift.Emuera.UI.Game.Image;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinorShift.Emuera.Runtime.Utils.EvilMask;
using MixedNum = MinorShift.Emuera.Runtime.Utils.EvilMask.Utils.MixedNum;
using StringStyle = MinorShift.Emuera.UI.Game.StringStyle;

namespace MinorShift.Emuera.GameView;

// ---- Console state enum ----
internal enum ConsoleState { Initializing = 0, Quit = 5, Error = 6, Running = 7, WaitInput = 20, Sleep = 21 }

// ---- Display update request enum ----
internal enum ConsoleRedraw { None = 0, Normal = 1 }

// ---- Window-level stubs (Console.Window.xxx access from scripts) ----

internal sealed class ConsoleWindowTextBox
{
    public string Text { get; set; } = "";
}

internal sealed class HotkeyStateStub
{
    public void HotkeyStateSet(nint a, nint b) { }
    public void HotkeyStateInit(nint a) { }
}

internal sealed class ConsolePictureBox
{
    public int Width  => 800;
    public int Height => 600;
    public Point PointToClient(Point p) => p;
    public Rectangle ClientRectangle => new Rectangle(0, 0, Width, Height);
    public bool Contains(Point p) => ClientRectangle.Contains(p);
}

internal sealed class ConsoleWindow
{
    public ConsoleWindowTextBox TextBox { get; } = new();
    public HotkeyStateStub hotkeyState { get; } = new();
    public ConsolePictureBox MainPicBox { get; } = new();
    public void ApplyTextBoxChanges() { }
    public void ResetTextBoxPos() { }
    public void SetTextBoxPos(int x, int y, int w) { }
    public void ChangeTextBox(string s) { }
}

// ---- Main adapter class ----

internal sealed class EmueraConsole
{
    private readonly IGameConsole _inner;
    private StringStyle _stringStyle;
    private EraColor _bgColor;
    private DisplayLineAlignment _alignment;
    private bool _mesSkip;
    private bool _useSetColorStyle;
    private bool _useUserStyle;
    private ConsoleRedraw _redraw = ConsoleRedraw.Normal;
    private readonly List<ConsoleDisplayLine> _displayLineList = [];
    private readonly ConsoleWindow _window = new();
    private readonly List<BackgroundLayer> _backgroundLayers = [];
    private GraphicsImage _cbgButtonMap;

    // Line state tracking for EmptyLine / LastLineIsEmpty / LineCount
    private long _lineCount = 0;
    private bool _currentLineIsEmpty = true;
    private bool _lastLineIsEmpty = true;
    private bool _lastInputWasTimeout = false;

    private sealed class BackgroundLayer
    {
        public string Name { get; init; } = string.Empty;
        public int Depth { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public bool IsButton { get; init; }
    }

    public EmueraConsole(IGameConsole inner)
    {
        _inner = inner;
        _stringStyle = new StringStyle(Config.ForeColor, FontStyle.Regular, Config.FontName);
        _bgColor = Config.BackColor;
        PrintBuffer = new PrintStringBuffer(() => _currentLineIsEmpty);
    }

    // ---- Core properties ----

    public StringStyle StringStyle => _stringStyle;
    public EraColor bgColor => _bgColor;
    public DisplayLineAlignment Alignment { get => _alignment; set => _alignment = value; }
    public bool MesSkip { get => _mesSkip; set => _mesSkip = value; }
    public bool UseSetColorStyle { get => _useSetColorStyle; set => _useSetColorStyle = value; }
    public bool UseUserStyle { get => _useUserStyle; set => _useUserStyle = value; }
    public bool IsRunning => _inner.IsRunning;
    public bool Enabled => true;
    public bool RunERBFromMemory => false;
    public bool EmptyLine => _currentLineIsEmpty;
    public bool LastLineIsEmpty => _lastLineIsEmpty;
    public bool LastLineIsTemporary => false;
    public long LastButtonGeneration => 0;
    public bool bitmapCacheEnabledForNextLine { get; set; }
    public int GetLineNo => (int)_lineCount;
    public bool noOutputLog { get; set; }
    public bool updatedGeneration { get; set; }
    public bool AlwaysRefresh { get; set; }
    public bool IsActive => true;
    public ConsoleRedraw Redraw => _redraw;
    public List<ConsoleDisplayLine> DisplayLineList => _displayLineList;
    public long ClientWidth => 800L;
    public long ClientHeight => 600L;
    public ConsoleWindow Window => _window;
    public ConsoleButtonString PointingSring => null;

    // ---- Build Gemuera.Bridge.StringStyle from current internal state ----
    private Gemuera.Bridge.StringStyle ToBridgeStyle()
    {
        var s = new Gemuera.Bridge.StringStyle
        {
            ForeArgb = _stringStyle.Color.ToArgb()
        };
        if ((_stringStyle.FontStyle & FontStyle.Bold)      != 0) s.Flags |= TextStyleFlags.Bold;
        if ((_stringStyle.FontStyle & FontStyle.Italic)    != 0) s.Flags |= TextStyleFlags.Italic;
        if ((_stringStyle.FontStyle & FontStyle.Strikeout) != 0) s.Flags |= TextStyleFlags.Strike;
        if ((_stringStyle.FontStyle & FontStyle.Underline) != 0) s.Flags |= TextStyleFlags.Underline;
        return s;
    }

    // ---- Print methods ----

    public void Print(string str)
    {
        if (!string.IsNullOrEmpty(str))
        {
            _inner.PrintString(str, ToBridgeStyle());
            _currentLineIsEmpty = false;
        }
    }

    // NOTE: lineEnd is an internal print-buffer hint in the original Emuera (PrintStringBuffer.Append lineEnd param).
    // It does NOT mean "add a newline here". NewLine() is called explicitly by OutputToConsole when IsNewLine()/IsWaitInput() is true.
    // Calling NewLine() here caused every PRINT to add a spurious newline, breaking multi-column output (e.g. roguelike ASCII art maps).
    public void Print(string str, bool lineEnd) { Print(str); }

    public PrintStringBuffer PrintBuffer { get; }

    public Dictionary<long, List<AConsoleDisplayNode>> EscapedParts { get; } = new Dictionary<long, List<AConsoleDisplayNode>>();
    public void Await(int ms) => System.Threading.Thread.Sleep(ms);

    public void PrintSingleLine(string str, bool temporary = false) { Print(str); NewLine(); }

    public void PrintSystemLine(string str) { Print(str); NewLine(); }

    public void PrintPlain(string str) => Print(str);

    public void PrintC(string str, bool isRight)
    {
        if (!string.IsNullOrEmpty(str))
        {
            var style = ToBridgeStyle();
            style.Align = isRight ? 2 : 1; // 1=center, 2=right
            _inner.PrintString(str, style);
            _currentLineIsEmpty = false;
        }
    }

    public void PrintButton(string display, long input)
    {
        var style = ToBridgeStyle();
        _inner.PrintButton(display, input.ToString(), style);
        AddButtonToDisplayHistory(display, input);
        _currentLineIsEmpty = false;
    }

    public void PrintButton(string display, string input)
    {
        var style = ToBridgeStyle();
        _inner.PrintButton(display, input, style);
        AddButtonToDisplayHistory(display, input);
        _currentLineIsEmpty = false;
    }

    public void PrintButtonC(string display, long input, bool isRight)
    {
        var style = ToBridgeStyle();
        style.Align = isRight ? 2 : 1;
        _inner.PrintButton(display, input.ToString(), style);
        AddButtonToDisplayHistory(display, input);
        _currentLineIsEmpty = false;
    }

    public void PrintButtonC(string display, string input, bool isRight)
    {
        var style = ToBridgeStyle();
        style.Align = isRight ? 2 : 1;
        _inner.PrintButton(display, input, style);
        AddButtonToDisplayHistory(display, input);
        _currentLineIsEmpty = false;
    }

    // BINPUT/BINPUTS in the core checks DisplayLineList for at least one button.
    // Keep a lightweight in-memory history entry so those checks behave on the Godot adapter.
    private void AddButtonToDisplayHistory(string display, long input)
    {
        var button = new ConsoleButtonString([], input);
        var line = new ConsoleDisplayLine([button], true, false)
        {
            LineNo = (int)_lineCount
        };
        _displayLineList.Add(line);
    }

    private void AddButtonToDisplayHistory(string display, string input)
    {
        var button = new ConsoleButtonString([], input ?? string.Empty);
        var line = new ConsoleDisplayLine([button], true, false)
        {
            LineNo = (int)_lineCount
        };
        _displayLineList.Add(line);
    }

    public void PrintTemporaryLine(string str) { Print(str); NewLine(); }

    public void PrintBar() => _inner.PrintLine(Config.DrawLineString);

    public void printCustomBar(string chars, bool isCustomDraw) => _inner.PrintLine(chars ?? Config.DrawLineString);

    public void PrintFlush(bool force = false)
    {
        // In the original Emuera, PrintFlush renders the current partial line.
        // In Gemuera we render immediately, so we just ensure the line ends.
        if (!_currentLineIsEmpty || force)
            NewLine();
    }

    public void PrintError(string str)
    {
        Gemuera.GemueraLogger.LogError($"[ScriptError] {str}");
        PrintSystemLine(str);
    }

    public void PrintErrorButton(string str, object pos, int? level = null)
    {
        Gemuera.GemueraLogger.LogError($"[ScriptError] {str}");
        PrintSystemLine(str);
    }

    public void PrintWarning(string str, object pos = null, int level = 0)
    {
        Gemuera.GemueraLogger.LogWarn($"[ScriptWarning] {str}");
        PrintSystemLine(str);
    }

    public void PrintHtml(string html, bool opt = false) => _inner.PrintHtml(html);

    public void PrintHTMLIsland(string str) { }

    public void ClearHTMLIsland() { }

    public void PrintImg(string name, string nameb, string namem, MixedNum height, MixedNum width, MixedNum depth)
    {
        // Phase 2 bridge: map PRINT_IMG to the Godot inline image API.
        // nameb/namem/depth are for extended image modes and are currently ignored.
        if (string.IsNullOrWhiteSpace(name))
            return;

        int imgWidth = (width?.num ?? 0);
        int imgHeight = (height?.num ?? 0);
        int align = _alignment switch
        {
            DisplayLineAlignment.CENTER => 1,
            DisplayLineAlignment.RIGHT => 2,
            _ => 0,
        };

        _inner.PrintImage(name, imgWidth, imgHeight, align);
        _currentLineIsEmpty = false;
    }

    public void PrintShape(string shape, MixedNum[] param) { }

    public void DebugPrint(string text) => _inner.DebugPrint(text);

    public void DebugNewLine() { }

    public void DebugClear() { }

    // Trace-log hooks (DebugMode-only in ProcessState)
    public void DebugClearTraceLog() { }
    public void DebugAddTraceLog(string trace) { }
    public void DebugRemoveTraceLog() { }

    // ---- Newline / line delete ----

    public void NewLine()
    {
        _inner.PrintNewLine();
        _lastLineIsEmpty = _currentLineIsEmpty;
        _currentLineIsEmpty = true;
        _lineCount++;
    }

    public void deleteLine(int count)
    {
        _inner.ClearLine(count);
        _lineCount = System.Math.Max(0, _lineCount - count);
        if (count <= 0) return;
        int remove = System.Math.Min(count, _displayLineList.Count);
        if (remove > 0)
            _displayLineList.RemoveRange(_displayLineList.Count - remove, remove);
    }

    public void RefreshStrings(bool force) { }

    // ---- Style mutators ----

    public void SetStringStyle(EraColor color)
    {
        _stringStyle = new StringStyle(color, _stringStyle.ColorChanged, _stringStyle.ButtonColor,
            _stringStyle.FontStyle, _stringStyle.Fontname);
    }

    public void SetStringStyle(FontStyle flags)
    {
        _stringStyle = new StringStyle(_stringStyle.Color, _stringStyle.ColorChanged, _stringStyle.ButtonColor,
            flags, _stringStyle.Fontname);
    }

    public void SetBgColor(EraColor c)
    {
        _bgColor = c;
        _inner.SetBackColor(c.ToArgb());
    }

    public void SetFont(string name)
    {
        _stringStyle = new StringStyle(_stringStyle.Color, _stringStyle.ColorChanged, _stringStyle.ButtonColor,
            _stringStyle.FontStyle, name);
    }

    public void ResetStyle()
    {
        _stringStyle = new StringStyle(Config.ForeColor, FontStyle.Regular, Config.FontName);
        _inner.ResetColors();
    }

    // ---- Input ----

    public void WaitInput(InputRequest req)
    {
        var result = _inner.RequestInputAsync(req).GetAwaiter().GetResult();
        if (result == null) return;
        _lastInputWasTimeout = result.IsTimeout;
        ApplyInputResult(req, result);
    }

    /// <summary>
    /// After the UI returns an InputResult, write it into the appropriate ERA variables.
    /// For IsSystemInput requests, also forwards the value to Process.InputSystemInteger
    /// so that systemResult (used by the system state machine) is updated.
    /// </summary>
    private static void ApplyInputResult(InputRequest req, Gemuera.Bridge.InputResult result)
    {
        var vev = GlobalStatic.VEvaluator;
        if (vev == null) return;

        // Determine the raw string: timeout uses default, otherwise the typed/clicked value.
        string raw = result.IsTimeout
            ? (req.InputType == InputType.StrValue ? (req.DefStrValue ?? "") : req.DefIntValue.ToString())
            : (result.Value ?? "");

        switch (req.InputType)
        {
            case InputType.StrValue:
            case InputType.StrButton:
                vev.RESULTS = raw;
                break;

            case InputType.IntValue:
            case InputType.IntButton:
            case InputType.AnyKey:
            case InputType.EnterKey:
            {
                long val = 0;
                long.TryParse(raw, out val);
                if (req.MouseInput)
                {
                    // Mouse/button index stored in RESULT:1
                    vev.RESULT_ARRAY[1] = val;
                }
                else
                {
                    vev.RESULT = val;
                }
                if (req.IsSystemInput)
                    GlobalStatic.Process?.InputSystemInteger(val);
                break;
            }

            case InputType.AnyValue:
                if (long.TryParse(raw, out long ival))
                {
                    vev.RESULT = ival;
                    if (req.IsSystemInput)
                        GlobalStatic.Process?.InputSystemInteger(ival);
                }
                else
                {
                    vev.RESULTS = raw;
                }
                break;

            case InputType.PrimitiveMouseKey:
            {
                // INPUTMOUSEKEY: store event data in RESULT_ARRAY[0..5].
                // RESULT:5 carries the sampled CBG button-map RGB value (-1 when none).
                // RESULT:6 is reserved for future extension (currently 0).
                vev.RESULT_ARRAY[0] = result.IsTimeout ? 0 : result.MouseType;
                vev.RESULT_ARRAY[1] = result.IsTimeout ? 0 : result.MouseButton;
                vev.RESULT_ARRAY[2] = result.IsTimeout ? 0 : result.MouseX;
                vev.RESULT_ARRAY[3] = result.IsTimeout ? 0 : result.MouseY;
                vev.RESULT_ARRAY[4] = result.IsTimeout ? 0 : result.MouseButtonCount;
                vev.RESULT_ARRAY[5] = result.IsTimeout ? 0 : result.MouseExtra;
                break;
            }

            // Void: no variable to update
        }
    }

    public void ReadAnyKey(bool canSkip = false, bool isEnterOnly = false)
    {
        var req = new InputRequest
        {
            InputType = isEnterOnly ? InputType.EnterKey : InputType.AnyKey
        };
        WaitInput(req);
    }

    // ---- State ----

    public void ClearText()
    {
        _inner.ClearScreen();
        _lineCount = 0;
        _currentLineIsEmpty = true;
        _lastLineIsEmpty = true;
        _displayLineList.Clear();
    }

    public void Quit() => _inner.Quit();

    public void ForceQuit() => _inner.Quit();

    public void ReloadErbFinished() { }

    public ConsoleDisplayLine[] GetDisplayLines(long lineNo) => [];

    public ConsoleDisplayLine[] PopDisplayingLines() => [];

    // ---- Window / title ----

    public void SetWindowTitle(string title) => _inner.SetWindowTitle(title);

    public string GetWindowTitle() => _inner.GetWindowTitle();

    public long LineCount => _lineCount;

    public bool IsTimeOut => _lastInputWasTimeout;

    public string getDefStBar() => string.Empty;

    public void setStBar(string bar) => _inner.SetStatusBar(bar);

    public string getStBar(string rowStr) => rowStr ?? string.Empty;

    public void OutputSystemLog(string path) { }

    public void ThrowError(bool playSound) { }

    public void ThrowTitleError(bool hasError) { }

    public void OutputLog(string filename, bool hideInfo) { }

    public void setRedrawTimer(int ms) { }

    public void SetRedraw(long val) { _redraw = val != 0 ? ConsoleRedraw.Normal : ConsoleRedraw.None; }

    public Point GetMousePosition()
    {
        var pos = _inner.GetMousePosition();
        return new Point(pos.X, pos.Y);
    }

    public void MoveMouse(Point p) { }

    // ---- CBG (client background graphics) ----

    private void RebuildBackgroundLayers()
    {
        _inner.CbgClear();
        foreach (var layer in _backgroundLayers.OrderBy(v => v.Depth))
            _inner.CbgSet(layer.Name, layer.X, layer.Y, layer.Width, layer.Height);
    }

    private void AddCbgLayer(string path, int x, int y, int width, int height, int depth, bool isButton)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        _backgroundLayers.Add(new BackgroundLayer
        {
            Name = path,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Depth = depth,
            IsButton = isButton,
        });
        RebuildBackgroundLayers();
    }

    public void AddBackgroundImage(string name, int depth, int opacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        _backgroundLayers.RemoveAll(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
        AddCbgLayer(name, 0, 0, 0, 0, depth, false);
    }

    public void RemoveBackground(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (_backgroundLayers.RemoveAll(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)) > 0)
            RebuildBackgroundLayers();
    }

    public void ClearBackgroundImage()
    {
        _backgroundLayers.RemoveAll(v => !v.IsButton);
        RebuildBackgroundLayers();
    }

    public void CBG_Clear()
    {
        _backgroundLayers.Clear();
        CBG_ClearBMap();
        _inner.CbgClear();
    }

    public void CBG_ClearRange(int zmin, int zmax)
    {
        if (zmin > zmax)
            return;

        _backgroundLayers.RemoveAll(v => v.Depth != 0 && v.Depth >= zmin && v.Depth <= zmax);
        RebuildBackgroundLayers();
    }

    public void CBG_ClearButton()
    {
        _backgroundLayers.RemoveAll(v => v.IsButton);
        CBG_ClearBMap();
        RebuildBackgroundLayers();
    }

    public void CBG_ClearBMap()
    {
        _cbgButtonMap = null;
        _inner.ClearCbgButtonMap();
    }

    public void CBG_SetGraphics(GraphicsImage g, int x, int y, int z)
    {
        if (g == null || !g.IsCreated || g.Bitmap == null)
            return;

        string path = g.Bitmap.SourcePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        AddCbgLayer(path, x, y, g.Width, g.Height, z, false);
    }

    public void CBG_SetButtonMap(GraphicsImage g)
    {
        if (g == null || !g.IsCreated || g.Bitmap == null)
            return;

        _cbgButtonMap = g;

        string path = g.Bitmap.SourcePath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            _inner.SetCbgButtonMap(path, g.Width, g.Height);
    }

    private static bool TryGetSpriteResource(ASprite sprite, out string resourcePath)
    {
        resourcePath = string.Empty;
        if (sprite is IResourceBackedSprite rs && !string.IsNullOrWhiteSpace(rs.ResourcePath))
        {
            resourcePath = rs.ResourcePath;
            return true;
        }
        return false;
    }

    public bool CBG_SetImage(object img, int x, int y, int z)
    {
        if (img is not ASprite sprite || !sprite.IsCreated)
            return false;
        if (!TryGetSpriteResource(sprite, out string path))
            return false;

        AddCbgLayer(path, x, y, sprite.DestBaseSize.Width, sprite.DestBaseSize.Height, z, false);
        return true;
    }

    public bool CBG_SetButtonImage(int b, ASprite imgN, ASprite imgB, int x, int y, int z, string tooltip)
    {
        if (imgN == null || !imgN.IsCreated)
            return false;
        if (!TryGetSpriteResource(imgN, out string path))
            return false;

        AddCbgLayer(path, x, y, imgN.DestBaseSize.Width, imgN.DestBaseSize.Height, z, true);
        return true;
    }

    // ---- Tooltip (all no-ops on non-Windows) ----

    public void SetToolTipColor(EraColor fc, EraColor bc) { }

    public void SetToolTipDelay(int ms) { }

    public void SetToolTipDuration(int ms) { }

    public void SetToolTipFontName(string name) { }

    public void SetToolTipFontSize(int size) { }

    public void SetToolTipFormat(long flags) { }

    public void SetToolTipImg(bool useImg) { }

    public void CustomToolTip(bool enabled) { }
}

/// <summary>Stub for the original PrintStringBuffer.</summary>
internal sealed class PrintStringBuffer
{
    private readonly Func<bool> _isLineEmpty;

    public PrintStringBuffer(Func<bool> isLineEmpty)
    {
        _isLineEmpty = isLineEmpty;
    }

    public bool IsEmpty => _isLineEmpty();
}
