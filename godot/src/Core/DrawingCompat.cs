// DrawingCompat.cs – Cross-platform stub implementations of System.Drawing types.
// Allows ported ERA interpreter code to compile without System.Drawing.Common.
// All GDI+ rendering operations are no-ops; actual rendering goes through Godot.
//
// NOTE: System.Drawing.Color IS defined here (not aliased to EraColor) so that
//       files using 'using System.Drawing;' get the Color type from here.
//       ConsoleDivPart.cs and Shape.cs use EraColor directly for pass-through.

namespace System.Drawing
{
    using Drawing2D;

    // -----------------------------------------------------------------------
    // FontStyle
    // -----------------------------------------------------------------------
    [Flags]
    internal enum FontStyle { Regular = 0, Bold = 1, Italic = 2, Underline = 4, Strikeout = 8 }

    // -----------------------------------------------------------------------
    // Color (minimal — only what the ported code calls)
    // -----------------------------------------------------------------------
    internal struct Color
    {
        private readonly int _argb;
        private Color(int argb) { _argb = argb; }

        public static Color FromArgb(int argb) => new Color(argb);
        public static Color FromArgb(int r, int g, int b) => new Color(unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b);
        public static Color FromArgb(int a, int r, int g, int b) => new Color((a << 24) | (r << 16) | (g << 8) | b);

        // Named color table (subset used by ERA games)
        public static Color FromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return Transparent;
            return name.ToLowerInvariant() switch
            {
                "black"   => Black, "white"  => White,  "red"    => Red,
                "green"   => Green, "blue"   => Blue,   "yellow" => FromArgb(255, 255, 0),
                "cyan"    => FromArgb(0, 255, 255), "magenta" => FromArgb(255, 0, 255),
                "gray" or "grey" => FromArgb(128, 128, 128),
                "silver"  => FromArgb(192, 192, 192),
                "orange"  => FromArgb(255, 165, 0),
                "purple"  => FromArgb(128, 0, 128),
                "pink"    => FromArgb(255, 192, 203),
                "brown"   => FromArgb(165, 42, 42),
                "transparent" => Transparent,
                _ => Transparent,
            };
        }
        public int ToArgb() => _argb;
        public byte A => (byte)((_argb >> 24) & 0xFF);
        public byte R => (byte)((_argb >> 16) & 0xFF);
        public byte G => (byte)((_argb >> 8) & 0xFF);
        public byte B => (byte)(_argb & 0xFF);

        public static readonly Color Transparent = new Color(0);
        public static readonly Color White  = FromArgb(255, 255, 255, 255);
        public static readonly Color Black  = FromArgb(255, 0, 0, 0);
        public static readonly Color Red    = FromArgb(255, 255, 0, 0);
        public static readonly Color Green  = FromArgb(255, 0, 255, 0);
        public static readonly Color Blue   = FromArgb(255, 0, 0, 255);

        public bool IsEmpty => _argb == 0;

        // Implicit conversions to/from EraColor for bridge use
        public static implicit operator MinorShift.Emuera.EraColor(Color c) =>
            MinorShift.Emuera.EraColor.FromArgb(c._argb);
        public static implicit operator Color(MinorShift.Emuera.EraColor e) =>
            new Color(e.ToArgb());

        public override string ToString() => $"Color#{_argb:X8}";
    }

    // -----------------------------------------------------------------------
    // Point / Size / Rectangle
    // -----------------------------------------------------------------------
    internal struct Point
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
        public static readonly Point Empty = new Point(0, 0);
        public void Offset(int dx, int dy) { X += dx; Y += dy; }
        public void Offset(Point p) { X += p.X; Y += p.Y; }
    }

    internal struct Size
    {
        public int Width, Height;
        public Size(int w, int h) { Width = w; Height = h; }
        public static readonly Size Empty = new Size(0, 0);
    }

    internal struct Rectangle
    {
        public int X, Y, Width, Height;
        public Rectangle(int x, int y, int w, int h) { X = x; Y = y; Width = w; Height = h; }
        public int Right  => X + Width;
        public int Bottom => Y + Height;
        public bool Contains(Point p) => p.X >= X && p.Y >= Y && p.X < X + Width && p.Y < Y + Height;
        public bool Contains(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
        public bool IntersectsWith(Rectangle r) => X < r.Right && Right > r.X && Y < r.Bottom && Bottom > r.Y;
        public static Rectangle Empty => new Rectangle(0, 0, 0, 0);
    }

    // -----------------------------------------------------------------------
    // Image (abstract base  Bitmap inherits from this)
    // -----------------------------------------------------------------------
    internal abstract class Image : IDisposable
    {
        public int Width  { get; protected set; }
        public int Height { get; protected set; }
        public abstract void Dispose();
    }

    // -----------------------------------------------------------------------
    // Brush / Pen / SolidBrush / Bitmap / Font / FontFamily / Region
    // -----------------------------------------------------------------------
    internal abstract class Brush : IDisposable
    {
        public abstract void Dispose();
    }

    internal sealed class SolidBrush : Brush
    {
        // Accept both System.Drawing.Color AND EraColor (Shape.cs passes EraColor)
        public SolidBrush(Color c)  { Color = c; }
        public SolidBrush(MinorShift.Emuera.EraColor c) { Color = (Color)c; }
        public Color Color { get; set; }
        public MinorShift.Emuera.EraColor EraColor => (MinorShift.Emuera.EraColor)Color;
        public override void Dispose() { }
    }

    internal sealed class Pen : IDisposable
    {
        public Pen(Color c) { Color = c; }
        public Pen(Color c, float width) { Color = c; Width = width; }
        public Pen(MinorShift.Emuera.EraColor c) { Color = (Color)c; }
        public Color Color { get; set; }
        public MinorShift.Emuera.EraColor EraColor => (MinorShift.Emuera.EraColor)Color;
        public float Width { get; set; } = 1f;
        public void Dispose() { }
    }

    internal sealed class Bitmap : Image
    {
        public string SourcePath { get; internal set; }

        public Bitmap(int w, int h)
        {
            Width = w;
            Height = h;
        }

        public Bitmap(int w, int h, Imaging.PixelFormat pf)
        {
            Width = w;
            Height = h;
        }

        public Bitmap(string filepath)
        {
            SourcePath = filepath;
            Width = 1;
            Height = 1;
        }

        public nint GetHicon() => nint.Zero;
        public void Save(string path) { }
        public void Save(System.IO.Stream s, Imaging.ImageFormat fmt) { }

        public override void Dispose()
        {
            Width = 0;
            Height = 0;
            SourcePath = null;
        }
    }

    internal sealed class Font : IDisposable
    {
        public string Name  { get; }
        public float  Size  { get; }
        public FontStyle Style { get; }
        public Font(string name, float size, FontStyle style = FontStyle.Regular)
        { Name = name; Size = size; Style = style; }
        public Font(string name, float size, FontStyle style, GraphicsUnit unit)
        { Name = name; Size = size; Style = style; }
        public Font(string name, float size, FontStyle style, FontFamily family)
        { Name = name; Size = size; Style = style; }
        public void Dispose() { }
    }

    internal sealed class FontFamily
    {
        public string Name { get; }
        public FontFamily(string name) { Name = name; }
    }

    internal sealed class Region : IDisposable
    {
        public Region(Rectangle r) { }
        public Region(Drawing2D.GraphicsPath path) { }
        public void Exclude(Rectangle r) { }
        public void Intersect(Rectangle r) { }
        public void Dispose() { }
    }

    // -----------------------------------------------------------------------
    // Icon
    // -----------------------------------------------------------------------
    internal sealed class Icon : IDisposable
    {
        public static Icon FromHandle(nint handle) => new Icon();
        public void Dispose() { }
    }

    // -----------------------------------------------------------------------
    // Graphics (all drawing methods are no-ops in stub phase)
    // -----------------------------------------------------------------------
    internal sealed class Graphics : IDisposable
    {
        public Drawing2D.SmoothingMode   SmoothingMode   { get; set; }
        public Drawing2D.PixelOffsetMode PixelOffsetMode { get; set; }

        public void FillRectangle(Brush b, Rectangle r)                                            { }
        public void FillRectangle(Brush b, int x, int y, int w, int h)                            { }
        public void FillPath(Brush b, Drawing2D.GraphicsPath path)                                 { }
        public void FillRegion(Brush b, Region region)                                             { }
        public void DrawString(string s, Font f, Brush b, float x, float y)                       { }
        public void DrawString(string s, Font f, Brush b, Point p)                                { }
        public void DrawString(string s, Font f, Brush b, float x, float y, StringFormat sf)      { }
        public SizeF MeasureString(string s, Font f) => new SizeF(s?.Length * f?.Size ?? 0, f?.Size ?? 0);
        public SizeF MeasureString(string s, Font f, int width, StringFormat sf) => MeasureString(s, f);
        public void DrawLine(Pen p, int x1, int y1, int x2, int y2)                               { }
        public void DrawLine(Pen p, Point a, Point b)                                              { }
        public void DrawRectangle(Pen p, Rectangle r)                                              { }
        public void DrawEllipse(Pen p, Rectangle r)                                                { }
        public void DrawImage(Bitmap src, Rectangle dst)                                           { }
        public void DrawImage(Bitmap src, Rectangle dst, Rectangle src2, GraphicsUnit u)           { }
        public void DrawImage(Image src, Rectangle dst)                                            { }
        public void SetClip(Rectangle r, Drawing2D.CombineMode m = Drawing2D.CombineMode.Replace)  { }
        public void SetClip(Drawing2D.GraphicsPath path, Drawing2D.CombineMode m = Drawing2D.CombineMode.Replace) { }
        public void SetClip(Region r, Drawing2D.CombineMode m = Drawing2D.CombineMode.Replace)    { }
        public void ResetClip()                                                                    { }
        public static Graphics FromImage(Image image) => new Graphics();
        public void Dispose() { }
    }

    // -----------------------------------------------------------------------
    // GraphicsUnit enum (used in DrawImage overload)
    // -----------------------------------------------------------------------
    internal enum GraphicsUnit { World=0, Display=1, Pixel=2, Point=3, Inch=4, Document=5, Millimeter=6 }

    internal struct SizeF
    {
        public float Width, Height;
        public SizeF(float w, float h) { Width = w; Height = h; }
    }

    internal sealed class StringFormat : IDisposable
    {
        public static readonly StringFormat GenericTypographic = new StringFormat();
        public void Dispose() { }
    }
}

// ---------------------------------------------------------------------------
// System.Drawing.Drawing2D
// ---------------------------------------------------------------------------
namespace System.Drawing.Drawing2D
{
    internal enum SmoothingMode   { Default = 0, None = 1, AntiAlias = 4, HighQuality = 2, HighSpeed = 1 }
    internal enum PixelOffsetMode { Default = 0, None = 1, HighQuality = 2, HighSpeed = 1 }
    internal enum CombineMode     { Replace = 0, Intersect = 1, Union = 2, Xor = 3, Exclude = 4, Complement = 5 }
    internal enum FillMode        { Alternate = 0, Winding = 1 }

    internal sealed class GraphicsPath : IDisposable
    {
        public void AddPolygon(Drawing.Point[] pts)                    { }
        public void AddEllipse(Drawing.Rectangle r)                    { }
        public void AddRectangle(Drawing.Rectangle r)                  { }
        public void AddPath(GraphicsPath other, bool connect)          { }
        public void CloseFigure()                                      { }
        public void Dispose() { }
    }

    internal sealed class LinearGradientBrush : Drawing.Brush
    {
        public LinearGradientBrush(Drawing.Rectangle r, Drawing.Color c1, Drawing.Color c2, float angle) { }
        public override void Dispose() { }
    }

    internal sealed class Matrix : IDisposable
    {
        public Matrix() { }
        public void Dispose() { }
    }
}

// ---------------------------------------------------------------------------
// System.Drawing.Imaging
// ---------------------------------------------------------------------------
namespace System.Drawing.Imaging
{
    internal enum PixelFormat { Format32bppArgb = 0, Format24bppRgb = 1, Format8bppIndexed = 2 }

    internal sealed class ImageFormat
    {
        public static ImageFormat Png  { get; } = new();
        public static ImageFormat Jpeg { get; } = new();
        public static ImageFormat Bmp  { get; } = new();
    }

    internal sealed class ColorMatrix
    {
        public float[][] Matrix { get; }
        public ColorMatrix() { Matrix = new float[5][]; for (int i = 0; i < 5; i++) Matrix[i] = new float[5]; }
        public ColorMatrix(float[][] m) { Matrix = m; }
        public static implicit operator ColorMatrix(float[][] m) => new ColorMatrix(m);
        public float this[int row, int col] { get => Matrix[row][col]; set => Matrix[row][col] = value; }
    }

    internal sealed class ImageAttributes : IDisposable
    {
        public void SetColorMatrix(ColorMatrix m) { }
        public void Dispose() { }
    }
}

// ---------------------------------------------------------------------------
// System.Drawing.Text
// ---------------------------------------------------------------------------
namespace System.Drawing.Text
{
    internal abstract class FontCollection : IDisposable
    {
        public virtual Drawing.FontFamily[] Families => Array.Empty<Drawing.FontFamily>();
        public void Dispose() { }
    }

    internal sealed class InstalledFontCollection : FontCollection { }
    internal sealed class PrivateFontCollection : FontCollection
    {
        public void AddFontFile(string path) { }
        public void AddMemoryFont(nint data, int length) { }
    }
}
