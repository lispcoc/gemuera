// Stub types for MinorShift.Emuera.UI.Game.Image namespace.
// Full image rendering is deferred to Phase 3.
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace MinorShift.Emuera.UI.Game.Image;

// ---------------------------------------------------------------------------
// AbstractImage  (base for GraphicsImage / ConstImage)
// ---------------------------------------------------------------------------

internal abstract class AbstractImage : IDisposable
{
    public const int MAX_IMAGESIZE = 8192;
    public abstract Bitmap Bitmap { get; set; }
    public abstract bool IsCreated { get; }
    public abstract void Dispose();
}

// ---------------------------------------------------------------------------
// GraphicsImage  (GCREATE target — in-memory drawing surface stub)
// ---------------------------------------------------------------------------

internal sealed class GraphicsImage : AbstractImage
{
    public readonly int ID;
    private Bitmap _bitmap;
    private bool _created;

    public GraphicsImage(int id) { ID = id; }

    public override Bitmap Bitmap { get => _bitmap; set => _bitmap = value; }
    public override bool IsCreated => _created;
    public int Width  => _bitmap?.Width  ?? 0;
    public int Height => _bitmap?.Height ?? 0;

    // Pen / Brush / Font stubs (used by GSetXxx, returned by property access)
    public Pen       Pen    { get; private set; } = new Pen(Color.White);
    public SolidBrush Brush { get; private set; } = new SolidBrush(Color.White);
    public Font      Fnt    { get; private set; } = new Font("Arial", 12);
    public string    Fontname  => Fnt?.Name ?? "";
    public float     Fontsize  => Fnt?.Size ?? 12f;
    public FontStyle Fontstyle => Fnt?.Style ?? FontStyle.Regular;

    // Graphics operations — all no-ops in stub phase
    public void GCreate(int w, int h, bool useGDI)            { _bitmap = new Bitmap(w, h); _created = true; }
    public void GCreateFromF(Bitmap bmp, bool useGDI)         { _bitmap = bmp; _created = bmp != null; }
    public void GDispose()                                     { _bitmap?.Dispose(); _bitmap = null; _created = false; }
    public void GClear(Color c)                                { }
    public void GClear(Color c, int x, int y, int w, int h)   { }
    public void GResize(int w, int h)                         { }
    public void GSetColor(Color c, int x, int y)              { }
    public void GSetBrush(Brush b)                            { Brush = (SolidBrush)b; }
    public void GSetFont(Font f, System.Drawing.FontStyle fs) { Fnt = f; }
    public void GSetPen(Pen p)                                { Pen = p; }
    public void GDashStyle(long style, long cap)              { }
    public void GDrawString(string text, int x, int y)        { }
    public void GDrawLine(int x1, int y1, int x2, int y2)     { }
    public void GFillRectangle(Rectangle rect)                { }
    public void GDrawG(GraphicsImage src, Rectangle dest, Rectangle srcRect)                 { }
    public void GDrawG(GraphicsImage src, Rectangle dest, Rectangle srcRect, ColorMatrix cm) { }
    public void GDrawGWithRotate(GraphicsImage src, double angle, int cx, int cy)            { }
    public void GDrawGWithMask(GraphicsImage src, GraphicsImage mask, Point dest)            { }
    public void GDrawCImg(ASprite img, Rectangle dest)                                       { }
    public void GDrawCImg(ASprite img, Rectangle dest, ColorMatrix cm)                       { }
    public EraColor GGetColor(int x, int y)                   => EraColor.Empty;

    public override void Dispose() => GDispose();
}

// ---------------------------------------------------------------------------
// ASprite  (abstract sprite — CroppedImage / SpriteAnime)
// ---------------------------------------------------------------------------

internal abstract class ASprite : IDisposable
{
    public abstract bool IsCreated { get; }
    public abstract Size DestBaseSize { get; }
    public abstract Point DestBasePosition { get; set; }
    public abstract EraColor SpriteGetColor(int x, int y);
    public abstract void AddFrame(GraphicsImage g, Rectangle rect, Point offset, int delay);
    public abstract void Dispose();
}

// ---------------------------------------------------------------------------
// CroppedImage / SpriteAnime (concrete ASprite stubs)
// ---------------------------------------------------------------------------

internal sealed class CroppedImage : ASprite
{
    private Point _pos;
    public CroppedImage(GraphicsImage src, Rectangle rect) { }
    public override bool IsCreated => false;
    public override Size DestBaseSize => Size.Empty;
    public override Point DestBasePosition { get => _pos; set => _pos = value; }
    public override EraColor SpriteGetColor(int x, int y) => EraColor.Empty;
    public override void AddFrame(GraphicsImage g, Rectangle rect, Point offset, int delay) { }
    public override void Dispose() { }
}

internal sealed class SpriteAnime : ASprite
{
    private Point _pos;
    public SpriteAnime(int w, int h) { }
    public override bool IsCreated => false;
    public override Size DestBaseSize => Size.Empty;
    public override Point DestBasePosition { get => _pos; set => _pos = value; }
    public override EraColor SpriteGetColor(int x, int y) => EraColor.Empty;
    public override void AddFrame(GraphicsImage g, Rectangle rect, Point offset, int delay) { }
    public override void Dispose() { }
}

// ---------------------------------------------------------------------------
// ConstImage  (resource image loaded from CSV)
// ---------------------------------------------------------------------------

internal sealed class ConstImage : AbstractImage
{
    public readonly string Name;
    public ConstImage(string name) { Name = name; }
    public override Bitmap Bitmap { get; set; }
    public override bool IsCreated => false;
    public override void Dispose() { Bitmap?.Dispose(); Bitmap = null; }
}

// ---------------------------------------------------------------------------
// AppContents  (static image registry — stubs return null / no-op)
// ---------------------------------------------------------------------------

internal static class AppContents
{
    private static readonly System.Collections.Generic.Dictionary<int, GraphicsImage> _graphics = [];
    public static System.Collections.Generic.HashSet<ConstImage> tempLoadedConstImages = [];
    public static System.Collections.Generic.HashSet<GraphicsImage> tempLoadedGraphicsImages = [];

    public static Exception LoadContents(bool reload) => null;

    public static GraphicsImage GetGraphics(int id)
    {
        if (!_graphics.TryGetValue(id, out var g))
        {
            g = new GraphicsImage(id);
            _graphics[id] = g;
        }
        return g;
    }

    public static ASprite GetSprite(string name) => null;

    public static void CreateSpriteG(string imgName, GraphicsImage parent, System.Drawing.Rectangle rect) { }

    public static void CreateSpriteAnime(string imgName, int w, int h) { }

    public static void SpriteDispose(string name) { }

    public static long SpriteDisposeAll(bool delCsvImage) => 0L;

    public static void UnloadContents() { _graphics.Clear(); }

    public static void UnloadTempLoadedConstImageNames()  { tempLoadedConstImages.Clear(); }
    public static void UnloadTempLoadedGraphicsImageNames() { tempLoadedGraphicsImages.Clear(); }
}
