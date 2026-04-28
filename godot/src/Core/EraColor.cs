// Minimal Color compatibility shim that replaces System.Drawing.Color
// throughout the ported interpreter core.
// Only the subset used by the config/statement files is implemented.
namespace MinorShift.Emuera;

internal struct EraColor : System.IEquatable<EraColor>
{
    private readonly int _argb;

    private EraColor(int argb) { _argb = argb; }

    public static EraColor FromArgb(int argb) => new EraColor(argb);
    public static EraColor FromArgb(int r, int g, int b) =>
        new EraColor(unchecked((int)0xFF000000 | (r << 16) | (g << 8) | b));
    public static EraColor FromArgb(int a, int r, int g, int b) =>
        new EraColor((a << 24) | (r << 16) | (g << 8) | b);

    /// <summary>Resolve a CSS/HTML color name.  Returns EraColor.Empty if unknown.</summary>
    public static EraColor FromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return Empty;
        var c = System.Drawing.Color.FromName(name);
        if (c.IsEmpty) return Empty;
        return new EraColor(c.ToArgb());
    }

    public int ToArgb() => _argb;

    public byte A => (byte)((_argb >> 24) & 0xFF);
    public byte R => (byte)((_argb >> 16) & 0xFF);
    public byte G => (byte)((_argb >>  8) & 0xFF);
    public byte B => (byte)( _argb        & 0xFF);

    // Common named colors used in Config defaults
    public static readonly EraColor Empty   = new EraColor(0);
    public static readonly EraColor Black   = FromArgb(0, 0, 0);
    public static readonly EraColor White   = FromArgb(255, 255, 255);
    public static readonly EraColor Gray    = FromArgb(128, 128, 128);
    public static readonly EraColor Silver  = FromArgb(192, 192, 192);
    public static readonly EraColor Yellow  = FromArgb(255, 255, 0);
    public static readonly EraColor Red     = FromArgb(255, 0, 0);
    public static readonly EraColor Green   = FromArgb(0, 255, 0);
    public static readonly EraColor Blue    = FromArgb(0, 0, 255);
    public static readonly EraColor Cyan    = FromArgb(0, 255, 255);
    public static readonly EraColor Magenta = FromArgb(255, 0, 255);
    public static readonly EraColor Transparent = new EraColor(0);

    public bool IsEmpty => _argb == 0;

    public bool Equals(EraColor other) => _argb == other._argb;
    public override bool Equals(object obj) => obj is EraColor c && Equals(c);
    public override int GetHashCode() => _argb;
    public static bool operator ==(EraColor a, EraColor b) => a._argb == b._argb;
    public static bool operator !=(EraColor a, EraColor b) => a._argb != b._argb;
    public override string ToString() => $"EraColor#{_argb:X8}";
}
