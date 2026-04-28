// Stub types for MinorShift.Emuera.UI.Game namespace.
// These provide the compile-time API surface needed by the ported Core files.
// Full rendering implementation is Phase 2-3.
using MinorShift.Emuera.Runtime.Config;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MinorShift.Emuera.UI.Game;

// ---------------------------------------------------------------------------
// StringStyle  (uses EraColor instead of System.Drawing.Color)
// ---------------------------------------------------------------------------

internal struct StringStyle
{
    public EraColor Color;
    public EraColor ButtonColor;
    public bool ColorChanged;
    public System.Drawing.FontStyle FontStyle;
    public string Fontname;

    public StringStyle(EraColor color, System.Drawing.FontStyle fontStyle, string fontname)
    {
        Color = color;
        ButtonColor = EraColor.White;
        ColorChanged = false;
        FontStyle = fontStyle;
        Fontname = string.IsNullOrEmpty(fontname) ? Config.FontName : fontname;
    }

    public StringStyle(EraColor color, bool colorChanged, EraColor buttonColor, System.Drawing.FontStyle fontStyle, string fontname)
    {
        Color = color;
        ButtonColor = buttonColor;
        ColorChanged = colorChanged;
        FontStyle = fontStyle;
        Fontname = string.IsNullOrEmpty(fontname) ? Config.FontName : fontname;
    }

    public override bool Equals(object obj)
    {
        if (obj is not StringStyle ss) return false;
        return Color == ss.Color && ButtonColor == ss.ButtonColor &&
               ColorChanged == ss.ColorChanged && FontStyle == ss.FontStyle &&
               string.Equals(Fontname, ss.Fontname, Config.SCIgnoreCase);
    }
    public override int GetHashCode() =>
        Color.GetHashCode() ^ ButtonColor.GetHashCode() ^ ColorChanged.GetHashCode() ^
        FontStyle.GetHashCode() ^ (Fontname?.GetHashCode() ?? 0);

    public static bool operator ==(StringStyle x, StringStyle y) =>
        x.Color == y.Color && x.ButtonColor == y.ButtonColor &&
        x.ColorChanged == y.ColorChanged && x.FontStyle == y.FontStyle &&
        string.Equals(x.Fontname, y.Fontname, Config.SCIgnoreCase);
    public static bool operator !=(StringStyle x, StringStyle y) => !(x == y);
}

// ---------------------------------------------------------------------------
// Alignment / line-state enums
// ---------------------------------------------------------------------------

internal enum DisplayLineLastState { None = 0, Normal = 1, Selected = 2, BackLog = 3 }

internal enum DisplayLineAlignment { LEFT = 0, CENTER = 1, RIGHT = 2 }

// ---------------------------------------------------------------------------
// AConsoleDisplayNode  (abstract base for all drawable console elements)
// ---------------------------------------------------------------------------

abstract class AConsoleDisplayNode
{
    public bool Error { get; protected set; }
    public string Text { get; protected set; }
    public string AltText { get; protected set; }
    public virtual int PointX { get; set; }
    public float XsubPixel { get; set; }
    public float WidthF { get; set; }
    public int Width { get; set; }
    public virtual int Top => 0;
    public virtual int Bottom => Config.FontSize;
    public abstract bool CanDivide { get; }

    // Signature matches the ported ConsoleDivPart override
    public abstract void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, bool isFocus, TextDrawingMode mode, bool isButton = false);

    public abstract void SetWidth(StringMeasure sm, float subPixel);

    public override string ToString() => Text ?? string.Empty;

    // EmuEra extensions
    public ConsoleButtonString Parent { get; set; }
    public int Depth { get; set; }
    public virtual StringBuilder BuildString(StringBuilder sb)
    {
        if (Text != null) sb.Append(Text);
        return sb;
    }

    // EmuEra-Rikaichan
    public bool rikaichaned;
    public int[] Ends;
    public AConsoleDisplayNode NextLine;
}

abstract class AConsoleColoredPart : AConsoleDisplayNode
{
    protected Color Color { get; set; }
    protected Color ButtonColor { get; set; }
    protected bool colorChanged;
}

// ---------------------------------------------------------------------------
// StringMeasure  (stub — GDI measurement not used in Godot)
// ---------------------------------------------------------------------------

class StringMeasure
{
    public float MeasureString(string text, System.Drawing.Font font) => 0f;
    public static float SubPixelAdjust(float w, float subPixel) => w;
}

// ---------------------------------------------------------------------------
// ConsoleButtonString  (one button / text span in a display line)
// ---------------------------------------------------------------------------

internal sealed class ConsoleButtonString
{
    public AConsoleDisplayNode[] StrArray { get; private set; }
    public int PointX { get; set; } = -1;
    public int Width { get; set; } = -1;
    public bool IsButton { get; private set; }
    public bool IsInteger { get; private set; }
    public long Input { get; private set; }
    public string Inputs { get; private set; }
    public long Generation { get; private set; }
    public ConsoleDisplayLine ParentLine { get; set; }
    public object ErrPos { get; set; }

    public ConsoleButtonString(AConsoleDisplayNode[] strs)
    {
        StrArray = strs;
        IsButton = false;
        PointX = -1;
        Width = -1;
    }

    public ConsoleButtonString(AConsoleDisplayNode[] strs, long input) : this(strs)
    {
        Input = input;
        Inputs = input.ToString();
        IsButton = true;
        IsInteger = true;
    }

    public ConsoleButtonString(AConsoleDisplayNode[] strs, string input) : this(strs)
    {
        Input = 0;
        Inputs = input;
        IsButton = true;
        IsInteger = false;
    }

    public override string ToString()
    {
        if (StrArray == null) return string.Empty;
        var sb = new StringBuilder();
        foreach (var node in StrArray) node?.BuildString(sb);
        return sb.ToString();
    }
}

// ---------------------------------------------------------------------------
// ConsoleDisplayLine  (one displayed line, holds ConsoleButtonStrings)
// ---------------------------------------------------------------------------

internal sealed class ConsoleDisplayLine
{
    private ConsoleButtonString[] buttons;
    private DisplayLineAlignment align;
    private bool aligned;

    public ConsoleDisplayLine(ConsoleButtonString[] buttons, bool isLogical, bool temporary, bool lineEnd = true)
    {
        this.buttons = buttons ?? [];
        foreach (var b in this.buttons)
            if (b != null) b.ParentLine = this;
        IsLogicalLine = isLogical;
        IsTemporary = temporary;
        IsLineEnd = lineEnd;
    }

    public int LineNo { get; set; } = -1;
    public bool IsLogicalLine { get; } = true;
    public bool IsTemporary { get; }
    public bool IsLineEnd { get; set; } = true;
    public bool bitmapCacheEnabled;

    public ConsoleButtonString[] Buttons => buttons;
    public DisplayLineAlignment Align => align;

    public void SetAlignment(DisplayLineAlignment align, int customWidth = -1)
    {
        if (aligned) return;
        aligned = true;
        this.align = align;
    }

    public void DrawTo(Graphics graph, int pointY, bool isBackLog, bool isFocus, TextDrawingMode mode) { }

    public void ShiftPositionX(int delta)
    {
        if (buttons == null) return;
        foreach (var b in buttons)
            if (b != null) b.PointX += delta;
    }

    public override string ToString()
    {
        if (buttons == null || buttons.Length == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var b in buttons) b?.BuildString(sb);
        return sb.ToString();
    }

    internal StringBuilder BuildString(StringBuilder sb)
    {
        if (buttons == null) return sb;
        foreach (var b in buttons) b?.BuildString(sb);
        return sb;
    }
}

// ConsoleButtonString extension for BuildString
internal static class ConsoleButtonStringExtension
{
    internal static StringBuilder BuildString(this ConsoleButtonString btn, StringBuilder sb)
    {
        if (btn?.StrArray == null) return sb;
        foreach (var node in btn.StrArray) node?.BuildString(sb);
        return sb;
    }
}

// ---------------------------------------------------------------------------
// HtmlManager  (stub tag/color helpers)
// ---------------------------------------------------------------------------

internal static class HtmlManager
{
    public static string[] HtmlTagSplit(string html)
    {
        if (string.IsNullOrEmpty(html)) return [];
        return [html];
    }

    public static string GetColorToString(EraColor c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static int HtmlLength(string html) => html?.Length ?? 0;

    public static string[] HtmlSubString(string html, int start) =>
        string.IsNullOrEmpty(html) ? (string[])(["", ""]) : new[] { html[..Math.Min(start, html.Length)], html[Math.Min(start, html.Length)..] };

    public static string DisplayLine2Html(ConsoleDisplayLine[] lines, bool withBr) =>
        string.Join(withBr ? "<br>" : "", System.Linq.Enumerable.Select(lines ?? [], l => l?.ToString() ?? ""));

    public static string Html2PlainText(string html) => html ?? "";

    public static string Escape(string text) =>
        text?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? "";
}
