// Stub types for MinorShift.Emuera.UI.Game.Image namespace.
// Full image rendering is deferred, but basic sprite registry behavior is implemented
// so SPRITE*/CBG* script paths can progress without always failing.
using MinorShift.Emuera;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MinorShift.Emuera.UI.Game.Image;

internal interface IResourceBackedSprite
{
    string ResourcePath { get; }
}

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
    public void GCreate(int w, int h, bool useGDI)
    {
        _bitmap = new Bitmap(w, h);
        _created = true;
    }

    public void GCreateFromF(Bitmap bmp, bool useGDI)
    {
        if (bmp == null)
        {
            _bitmap = null;
            _created = false;
            return;
        }

        // Keep an independent instance because callers may dispose the source bitmap immediately.
        _bitmap = new Bitmap(Math.Max(1, bmp.Width), Math.Max(1, bmp.Height))
        {
            SourcePath = bmp.SourcePath
        };
        _created = true;
    }

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

internal sealed class CroppedImage : ASprite, IResourceBackedSprite
{
    private Point _pos;
    private readonly Size _size;
    public string ResourcePath { get; }

    public CroppedImage(GraphicsImage src, Rectangle rect)
    {
        _size = new Size(Math.Max(0, rect.Width), Math.Max(0, rect.Height));
        ResourcePath = string.Empty;
    }

    public CroppedImage(string resourcePath, int width, int height)
    {
        ResourcePath = resourcePath ?? string.Empty;
        _size = new Size(Math.Max(1, width), Math.Max(1, height));
    }

    public override bool IsCreated => _size.Width > 0 && _size.Height > 0;
    public override Size DestBaseSize => _size;
    public override Point DestBasePosition { get => _pos; set => _pos = value; }
    public override EraColor SpriteGetColor(int x, int y) => EraColor.Empty;
    public override void AddFrame(GraphicsImage g, Rectangle rect, Point offset, int delay) { }
    public override void Dispose() { }
}

internal sealed class SpriteAnime : ASprite
{
    private Point _pos;
    private readonly Size _size;
    public SpriteAnime(int w, int h) { _size = new Size(Math.Max(1, w), Math.Max(1, h)); }
    public override bool IsCreated => _size.Width > 0 && _size.Height > 0;
    public override Size DestBaseSize => _size;
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
    public override bool IsCreated => Bitmap != null;
    public override void Dispose() { Bitmap?.Dispose(); Bitmap = null; }
}

// ---------------------------------------------------------------------------
// AppContents  (static image registry)
// ---------------------------------------------------------------------------

internal static class AppContents
{
    private static readonly Dictionary<int, GraphicsImage> _graphics = [];
    private static readonly Dictionary<string, ASprite> _sprites =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] _spriteExts = [".png", ".jpg", ".jpeg", ".webp", ".bmp"];

    public static HashSet<ConstImage> tempLoadedConstImages = [];
    public static HashSet<GraphicsImage> tempLoadedGraphicsImages = [];

    public static Exception LoadContents(bool reload)
    {
        if (reload)
            _sprites.Clear();
        return null;
    }

    public static GraphicsImage GetGraphics(int id)
    {
        if (!_graphics.TryGetValue(id, out var g))
        {
            g = new GraphicsImage(id);
            _graphics[id] = g;
        }
        return g;
    }

    private static string ResolveSpritePath(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string normalized = name.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized) && File.Exists(normalized))
            return normalized;

        if (Path.HasExtension(normalized))
        {
            string candidate = Path.Combine(Program.ContentDir, normalized);
            if (File.Exists(candidate))
                return candidate;
        }
        else
        {
            string basePath = Path.Combine(Program.ContentDir, normalized);
            foreach (string ext in _spriteExts)
            {
                string candidate = basePath + ext;
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    public static ASprite GetSprite(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_sprites.TryGetValue(name, out var existing) && existing != null && existing.IsCreated)
            return existing;

        string path = ResolveSpritePath(name);
        if (path == null)
            return null;

        var sprite = new CroppedImage(path, 1, 1);
        _sprites[name] = sprite;
        return sprite;
    }

    public static void CreateSpriteG(string imgName, GraphicsImage parent, Rectangle rect)
    {
        if (string.IsNullOrWhiteSpace(imgName) || parent == null || !parent.IsCreated)
            return;

        _sprites[imgName] = new CroppedImage(parent, rect);
    }

    public static void CreateSpriteAnime(string imgName, int w, int h)
    {
        if (string.IsNullOrWhiteSpace(imgName))
            return;
        _sprites[imgName] = new SpriteAnime(w, h);
    }

    public static void SpriteDispose(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (_sprites.TryGetValue(name, out var sprite))
            sprite?.Dispose();
        _sprites.Remove(name);
    }

    public static long SpriteDisposeAll(bool delCsvImage)
    {
        long count = _sprites.Count;
        foreach (var sprite in _sprites.Values)
            sprite?.Dispose();
        _sprites.Clear();
        return count;
    }

    public static void UnloadContents()
    {
        _graphics.Clear();
        SpriteDisposeAll(true);
    }

    public static void UnloadTempLoadedConstImageNames() { tempLoadedConstImages.Clear(); }
    public static void UnloadTempLoadedGraphicsImageNames() { tempLoadedGraphicsImages.Clear(); }
}
