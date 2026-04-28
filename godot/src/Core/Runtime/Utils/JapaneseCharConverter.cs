// Cross-platform replacement for Microsoft.VisualBasic.Strings.StrConv
// Handles fullwidth ↔ halfwidth character conversion for ERA STRFORM functions.
using System.Text;

namespace MinorShift.Emuera.Runtime.Utils;

internal static class JapaneseCharConverter
{
    // Halfwidth katakana range: U+FF65–U+FF9F
    // Fullwidth ASCII range:   U+FF01–U+FF5E  (maps to U+0021–U+007E)
    // Fullwidth space:         U+3000          (maps to U+0020)
    // Fullwidth katakana:      U+30A1–U+30F6 voiced/semi-voiced handled via combining dakuten

    private const char HalfKataStart = '\uFF65';
    private const char HalfKataEnd   = '\uFF9F';

    // Lookup: halfwidth katakana → fullwidth katakana (excluding voiced/semi-voiced)
    private static readonly char[] HalfToFullKata =
    {
        '・', 'ヲ', 'ァ', 'ィ', 'ゥ', 'ェ', 'ォ',  // FF65–FF6B
        'ャ', 'ュ', 'ョ', 'ッ', 'ー', 'ア', 'イ',  // FF6C–FF72
        'ウ', 'エ', 'オ', 'カ', 'キ', 'ク', 'ケ',  // FF73–FF79
        'コ', 'サ', 'シ', 'ス', 'セ', 'ソ', 'タ',  // FF7A–FF80
        'チ', 'ツ', 'テ', 'ト', 'ナ', 'ニ', 'ヌ',  // FF81–FF87
        'ネ', 'ノ', 'ハ', 'ヒ', 'フ', 'ヘ', 'ホ',  // FF88–FF8E
        'マ', 'ミ', 'ム', 'メ', 'モ', 'ヤ', 'ユ',  // FF8F–FF95
        'ヨ', 'ラ', 'リ', 'ル', 'レ', 'ロ', 'ワ',  // FF96–FF9C
        'ン', '゛', '゜'                           // FF9D–FF9F
    };

    // Voiced (dakuten) pairs: fullwidth base + ゛ → voiced form
    private static readonly (char base_, char voiced)[] DakutenPairs =
    {
        ('カ','ガ'),('キ','ギ'),('ク','グ'),('ケ','ゲ'),('コ','ゴ'),
        ('サ','ザ'),('シ','ジ'),('ス','ズ'),('セ','ゼ'),('ソ','ゾ'),
        ('タ','ダ'),('チ','ヂ'),('ツ','ヅ'),('テ','デ'),('ト','ド'),
        ('ハ','バ'),('ヒ','ビ'),('フ','ブ'),('ヘ','ベ'),('ホ','ボ'),
        ('ウ','ヴ'),
    };

    // Semi-voiced (handakuten) pairs
    private static readonly (char base_, char semiVoiced)[] HandakutenPairs =
    {
        ('ハ','パ'),('ヒ','ピ'),('フ','プ'),('ヘ','ペ'),('ホ','ポ'),
    };

    /// <summary>Convert fullwidth (zenkaku) characters to halfwidth (hankaku).</summary>
    public static string ToHalfWidth(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length * 2);
        foreach (char c in s)
        {
            // Fullwidth space → halfwidth space
            if (c == '\u3000') { sb.Append(' '); continue; }
            // Fullwidth ASCII printable → halfwidth
            if (c >= '\uFF01' && c <= '\uFF5E') { sb.Append((char)(c - 0xFEE0)); continue; }
            // Fullwidth katakana → halfwidth
            if (TryFullToHalfKata(c, out string half)) { sb.Append(half); continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Convert halfwidth (hankaku) characters to fullwidth (zenkaku).</summary>
    public static string ToFullWidth(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            // Halfwidth space → fullwidth
            if (c == ' ') { sb.Append('\u3000'); continue; }
            // Halfwidth ASCII printable → fullwidth
            if (c >= '!' && c <= '~') { sb.Append((char)(c + 0xFEE0)); continue; }
            // Halfwidth katakana
            if (c >= HalfKataStart && c <= HalfKataEnd)
            {
                char fw = HalfToFullKata[c - HalfKataStart];
                // Check for following dakuten / handakuten
                if (i + 1 < s.Length)
                {
                    char next = s[i + 1];
                    if (next == '\uFF9E') // halfwidth dakuten
                    {
                        char? voiced = ApplyDakuten(fw);
                        if (voiced.HasValue) { sb.Append(voiced.Value); i++; continue; }
                    }
                    else if (next == '\uFF9F') // halfwidth handakuten
                    {
                        char? sv = ApplyHandakuten(fw);
                        if (sv.HasValue) { sb.Append(sv.Value); i++; continue; }
                    }
                }
                sb.Append(fw);
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    // ---- Helpers ----

    private static bool TryFullToHalfKata(char c, out string half)
    {
        for (int i = 0; i < HalfToFullKata.Length; i++)
        {
            if (HalfToFullKata[i] == c)
            {
                half = ((char)(HalfKataStart + i)).ToString();
                return true;
            }
        }
        // Voiced forms → base + dakuten
        foreach (var (b, v) in DakutenPairs)
            if (c == v) { half = ToHalfWidth(b.ToString()) + '\uFF9E'; return true; }
        foreach (var (b, sv) in HandakutenPairs)
            if (c == sv) { half = ToHalfWidth(b.ToString()) + '\uFF9F'; return true; }
        half = null; return false;
    }

    private static char? ApplyDakuten(char c)
    {
        foreach (var (b, v) in DakutenPairs) if (b == c) return v;
        return null;
    }

    private static char? ApplyHandakuten(char c)
    {
        foreach (var (b, sv) in HandakutenPairs) if (b == c) return sv;
        return null;
    }

    // ---- Hiragana ↔ Katakana ----
    // Hiragana: U+3041–U+3096   Katakana: U+30A1–U+30F6

    /// <summary>Convert hiragana characters to katakana.</summary>
    public static string HiraganaToKatakana(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            sb.Append(c >= '\u3041' && c <= '\u3096' ? (char)(c + 0x60) : c);
        return sb.ToString();
    }

    /// <summary>Convert katakana characters to hiragana.</summary>
    public static string KatakanaToHiragana(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            sb.Append(c >= '\u30A1' && c <= '\u30F6' ? (char)(c - 0x60) : c);
        return sb.ToString();
    }
}
