// Stub types for MinorShift.Emuera.UI.Game.Image namespace.
// Full image rendering is deferred, but basic sprite registry behavior is implemented
// so SPRITE*/CBG* script paths can progress without always failing.
using Godot;
using MinorShift.Emuera;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using Bitmap = System.Drawing.Bitmap;

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
// GraphicsImage  (GCREATE target — in-memory drawing surface backed by Godot.Image)
// ---------------------------------------------------------------------------

internal sealed class GraphicsImage : AbstractImage
{
    public readonly int ID;
    private Bitmap _stubBitmap;
    private bool _created;

    // Godot.Image backing store for real pixel operations.
    private Godot.Image _surface;

    public GraphicsImage(int id) { ID = id; }

    public override Bitmap Bitmap { get => _stubBitmap; set => _stubBitmap = value; }
    public override bool IsCreated => _created;
    public int Width  => _surface?.GetWidth()  ?? 0;
    public int Height => _surface?.GetHeight() ?? 0;

    /// <summary>Access the underlying Godot.Image pixel surface (may be null if not created).</summary>
    public Godot.Image GodotSurface => _surface;

    // Pen / Brush / Font stubs (used by GSetXxx, returned by property access)
    public System.Drawing.Pen       Pen    { get; private set; } = new System.Drawing.Pen(System.Drawing.Color.White);
    public System.Drawing.SolidBrush Brush { get; private set; } = new System.Drawing.SolidBrush(System.Drawing.Color.White);
    public System.Drawing.Font      Fnt    { get; private set; } = new System.Drawing.Font("Arial", 12);
    public string    Fontname  => Fnt?.Name ?? "";
    public float     Fontsize  => Fnt?.Size ?? 12f;
    public FontStyle Fontstyle => Fnt?.Style ?? FontStyle.Regular;

    // ----------------------------------------------------------------
    // Creation / disposal
    // ----------------------------------------------------------------

    public void GCreate(int w, int h, bool useGDI)
    {
        GDispose();
        int pw = System.Math.Max(1, w);
        int ph = System.Math.Max(1, h);
        _surface = Godot.Image.CreateEmpty(pw, ph, false, Godot.Image.Format.Rgba8);
        _surface.Fill(new Godot.Color(0, 0, 0, 0));
        _stubBitmap = new Bitmap(pw, ph);
        _created = true;
        lock (AppContents.tempLoadedGraphicsImages)
            AppContents.tempLoadedGraphicsImages.Add(this);
    }

    public void GCreateFromF(Bitmap bmp, bool useGDI)
    {
        GDispose();
        if (bmp == null) return;

        string srcPath = bmp.SourcePath;
        if (!string.IsNullOrEmpty(srcPath) && System.IO.File.Exists(srcPath))
        {
            var img = new Godot.Image();
            if (img.Load(srcPath) == Error.Ok)
            {
                img.Convert(Godot.Image.Format.Rgba8);
                _surface = img;
                _stubBitmap = new Bitmap(img.GetWidth(), img.GetHeight()) { SourcePath = srcPath };
                _created = true;
                return;
            }
        }

        // Fallback: create blank surface with the stub dimensions.
        int pw = System.Math.Max(1, bmp.Width);
        int ph = System.Math.Max(1, bmp.Height);
        _surface = Godot.Image.CreateEmpty(pw, ph, false, Godot.Image.Format.Rgba8);
        _stubBitmap = new Bitmap(pw, ph) { SourcePath = srcPath };
        _created = true;
    }

    public void GDispose()
    {
        _surface = null;
        _stubBitmap?.Dispose();
        _stubBitmap = null;
        _created = false;
    }

    // ----------------------------------------------------------------
    // Drawing operations
    // ----------------------------------------------------------------

    public void GClear(System.Drawing.Color c)
    {
        if (_surface == null) return;
        _surface.Fill(EraColorToGodot(c));
    }

    public void GClear(System.Drawing.Color c, int x, int y, int w, int h)
    {
        if (_surface == null) return;
        _surface.FillRect(new Godot.Rect2I(x, y, w, h), EraColorToGodot(c));
    }

    public void GResize(int w, int h)
    {
        if (_surface == null) return;
        _surface.Resize(System.Math.Max(1, w), System.Math.Max(1, h));
    }

    public void GSetColor(System.Drawing.Color c, int x, int y)
    {
        if (_surface == null) return;
        _surface.SetPixel(x, y, EraColorToGodot(c));
    }

    public void GSetBrush(System.Drawing.Brush b)    { Brush = (System.Drawing.SolidBrush)b; }
    public void GSetFont(System.Drawing.Font f, System.Drawing.FontStyle fs) { Fnt = f; }
    public void GSetPen(System.Drawing.Pen p)         { Pen = p; }
    public void GDashStyle(long style, long cap)      { }

    public void GFillRectangle(System.Drawing.Rectangle rect)
    {
        if (_surface == null) return;
        Godot.Color gc = (Brush != null)
            ? EraColorToGodot(Brush.Color)
            : new Godot.Color(0, 0, 0, 1);
        _surface.FillRect(new Godot.Rect2I(rect.X, rect.Y, rect.Width, rect.Height), gc);
    }

    public void GDrawLine(int x1, int y1, int x2, int y2) { /* not implemented */ }

    public void GDrawString(string text, int x, int y)        { /* text rendering into image not implemented */ }

    public void GDrawG(GraphicsImage src, System.Drawing.Rectangle dest, System.Drawing.Rectangle srcRect)
    {
        if (_surface == null || src?._surface == null) return;
        _surface.BlitRect(src._surface,
            new Godot.Rect2I(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height),
            new Godot.Vector2I(dest.X, dest.Y));
    }

    public void GDrawG(GraphicsImage src, System.Drawing.Rectangle dest, System.Drawing.Rectangle srcRect, ColorMatrix cm)
    {
        // ColorMatrix modulation not implemented — fall through to plain blit.
        GDrawG(src, dest, srcRect);
    }

    public void GDrawGWithRotate(GraphicsImage src, double angle, int cx, int cy)
    {
        // Rotation into image not implemented.
    }

    public void GDrawGWithMask(GraphicsImage src, GraphicsImage mask, System.Drawing.Point dest)
    {
        if (_surface == null || src?._surface == null) return;
        var full = new Godot.Rect2I(0, 0, src._surface.GetWidth(), src._surface.GetHeight());
        _surface.BlitRect(src._surface, full, new Godot.Vector2I(dest.X, dest.Y));
    }

    public void GDrawCImg(ASprite img, System.Drawing.Rectangle dest)
    {
        if (_surface == null || img == null) return;
        if (img is IResourceBackedSprite rbs)
        {
            string path = rbs.ResourcePath;
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                var src = new Godot.Image();
                if (src.Load(path) == Error.Ok)
                {
                    src.Convert(Godot.Image.Format.Rgba8);
                    _surface.BlitRect(src,
                        new Godot.Rect2I(0, 0, src.GetWidth(), src.GetHeight()),
                        new Godot.Vector2I(dest.X, dest.Y));
                }
            }
        }
    }

    public void GDrawCImg(ASprite img, System.Drawing.Rectangle dest, ColorMatrix cm)
    {
        GDrawCImg(img, dest);
    }

    public EraColor GGetColor(int x, int y)
    {
        if (_surface == null) return EraColor.Empty;
        if (x < 0 || y < 0 || x >= _surface.GetWidth() || y >= _surface.GetHeight()) return EraColor.Empty;
        var c = _surface.GetPixel(x, y);
        return EraColor.FromArgb(
            (int)(c.A * 255), (int)(c.R * 255), (int)(c.G * 255), (int)(c.B * 255));
    }

    public override void Dispose() => GDispose();

    // ----------------------------------------------------------------
    // Utility
    // ----------------------------------------------------------------

    private static Godot.Color EraColorToGodot(System.Drawing.Color c)
        => new Godot.Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
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

    // Optional: source GraphicsImage and the region to crop from it.
    public GraphicsImage SourceGraphics { get; }
    public System.Drawing.Rectangle SourceRect { get; }

    public CroppedImage(GraphicsImage src, System.Drawing.Rectangle rect)
    {
        SourceGraphics = src;
        SourceRect = rect;
        _size = new Size(System.Math.Max(0, rect.Width), System.Math.Max(0, rect.Height));
        ResourcePath = string.Empty;
    }

    public CroppedImage(string resourcePath, int width, int height)
    {
        ResourcePath = resourcePath ?? string.Empty;
        _size = new Size(System.Math.Max(1, width), System.Math.Max(1, height));
    }

    public override bool IsCreated => _size.Width > 0 && _size.Height > 0;
    public override Size DestBaseSize => _size;
    public override Point DestBasePosition { get => _pos; set => _pos = value; }
    public override EraColor SpriteGetColor(int x, int y) => EraColor.Empty;
    public override void AddFrame(GraphicsImage g, System.Drawing.Rectangle rect, Point offset, int delay) { }
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
    public override void AddFrame(GraphicsImage g, System.Drawing.Rectangle rect, Point offset, int delay) { }
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

    private static readonly Regex _tailNumberRegex = new(@"^(.*)_(\d+)$", RegexOptions.Compiled);
    private static readonly Regex _middleNumberRegex = new(@"^(.*)_(\d+)_(.*)$", RegexOptions.Compiled);

    private static IEnumerable<string> BuildStemCandidates(string stem)
    {
        if (string.IsNullOrWhiteSpace(stem))
            yield break;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool Add(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return seen.Add(value);
        }

        if (Add(stem)) yield return stem;

        string noNormal = stem.Replace("_通常", string.Empty, StringComparison.Ordinal);
        if (Add(noNormal)) yield return noNormal;

        string eyeAlias = noNormal.Replace("瞳", "目", StringComparison.Ordinal);
        if (Add(eyeAlias)) yield return eyeAlias;

        Match mTail = _tailNumberRegex.Match(eyeAlias);
        if (mTail.Success && int.TryParse(mTail.Groups[2].Value, out int nTail))
        {
            string head = mTail.Groups[1].Value;
            string padded = $"{head}_{nTail:00}";
            if (Add(padded)) yield return padded;

            string paddedNext = $"{head}_{nTail + 1:00}";
            if (Add(paddedNext)) yield return paddedNext;

            string compact = head + nTail.ToString();
            if (Add(compact)) yield return compact;
        }

        Match mMid = _middleNumberRegex.Match(eyeAlias);
        if (mMid.Success && int.TryParse(mMid.Groups[2].Value, out int nMid))
        {
            string head = mMid.Groups[1].Value;
            string tail = mMid.Groups[3].Value;
            string compactMid = $"{head}{nMid}_{tail}";
            if (Add(compactMid)) yield return compactMid;

            string compactMidPadded = $"{head}{nMid:00}_{tail}";
            if (Add(compactMidPadded)) yield return compactMidPadded;
        }
    }

    private static string FindByFilenameRecursive(string root, string fileName)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(root))
            return null;

        try
        {
            string[] hits = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
            return hits.Length > 0 ? hits[0] : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveSpritePath(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string normalized = name.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized) && File.Exists(normalized))
            return normalized;

        string[] roots =
        {
            Program.ContentDir,
            Program.ExeDir,
        };

        bool hasExt = Path.HasExtension(normalized);
        string dirPart = Path.GetDirectoryName(normalized) ?? string.Empty;
        string stem = hasExt ? Path.GetFileNameWithoutExtension(normalized) : Path.GetFileName(normalized);
        string ext = hasExt ? Path.GetExtension(normalized) : string.Empty;

        foreach (string root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            if (hasExt)
            {
                string direct = Path.Combine(root, normalized);
                if (File.Exists(direct))
                    return direct;
            }
            else
            {
                string directNoExt = Path.Combine(root, normalized);
                foreach (string spriteExt in _spriteExts)
                {
                    string candidate = directNoExt + spriteExt;
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            foreach (string stemCandidate in BuildStemCandidates(stem))
            {
                if (hasExt)
                {
                    string relFile = string.IsNullOrEmpty(dirPart)
                        ? stemCandidate + ext
                        : Path.Combine(dirPart, stemCandidate + ext);
                    string rooted = Path.Combine(root, relFile);
                    if (File.Exists(rooted))
                        return rooted;

                    string recursiveHit = FindByFilenameRecursive(root, stemCandidate + ext);
                    if (recursiveHit != null)
                        return recursiveHit;
                }
                else
                {
                    foreach (string spriteExt in _spriteExts)
                    {
                        string relFile = string.IsNullOrEmpty(dirPart)
                            ? stemCandidate + spriteExt
                            : Path.Combine(dirPart, stemCandidate + spriteExt);
                        string rooted = Path.Combine(root, relFile);
                        if (File.Exists(rooted))
                            return rooted;

                        string recursiveHit = FindByFilenameRecursive(root, stemCandidate + spriteExt);
                        if (recursiveHit != null)
                            return recursiveHit;
                    }
                }
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

    public static void CreateSpriteG(string imgName, GraphicsImage parent, System.Drawing.Rectangle rect)
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
