// Shared data types used across the Bridge layer.
namespace Gemuera.Bridge;

// ----------------------------------------------------------------
// Text styling
// ----------------------------------------------------------------

/// <summary>
/// Text style flags for a printed string.
/// Replaces the original StringStyle / ConsoleStyledString colour logic.
/// </summary>
[System.Flags]
public enum TextStyleFlags
{
    Normal   = 0,
    Bold     = 1 << 0,
    Italic   = 1 << 1,
    Strike   = 1 << 2,
    Underline= 1 << 3,
}

/// <summary>
/// Complete style descriptor for a single text span.
/// </summary>
public sealed class StringStyle
{
    public TextStyleFlags Flags;
    /// <summary>Foreground color, packed ARGB. -1 = use default.</summary>
    public int ForeArgb = -1;
    /// <summary>Background highlight color, packed ARGB. -1 = none.</summary>
    public int BackArgb = -1;
    /// <summary>Text alignment: 0=left (default), 1=center, 2=right.</summary>
    public int Align = 0;

    public static readonly StringStyle Default = new();

    public StringStyle Clone() => (StringStyle)MemberwiseClone();
}

// ----------------------------------------------------------------
// Input result
// ----------------------------------------------------------------

/// <summary>
/// Result returned by IGameConsole.RequestInputAsync().
/// </summary>
public sealed class InputResult
{
    /// <summary>The raw string typed/selected by the user. Null on timeout.</summary>
    public string Value;

    /// <summary>True if the input timed out (TINPUT / TINPUTS).</summary>
    public bool IsTimeout;

    /// <summary>True if the user pressed a button (INPUTBUTTON).</summary>
    public bool IsButton;

    /// <summary>True if the result is from a mouse click.</summary>
    public bool IsMouse;

    /// <summary>Mouse X position in console coordinates (if IsMouse).</summary>
    public int MouseX;
    /// <summary>Mouse Y position in console coordinates (if IsMouse).</summary>
    public int MouseY;

    // ---- INPUTMOUSEKEY (PrimitiveMouseKey) fields ----
    /// <summary>
    /// INPUTMOUSEKEY event type:
    ///   0 = timeout / none
    ///   1 = mouse button press
    ///   2 = mouse wheel scroll
    ///   4 = keyboard key press
    /// </summary>
    public int MouseType;

    /// <summary>
    /// For type=1: Windows MouseButtons value (1=Left, 2=Right, 4=Middle).
    /// For type=2: scroll delta (+1 = up, -1 = down).
    /// For type=4: always 0.
    /// </summary>
    public int MouseButton;

    /// <summary>
    /// INPUTMOUSEKEY RESULT:5 payload.
    /// For button-map enabled CBG/HTML inputs this stores the 24-bit RGB map value,
    /// or -1 when no mapped pixel is under the cursor.
    /// </summary>
    public int MouseButtonCount;

    /// <summary>INPUTMOUSEKEY RESULT:6 payload (reserved, currently 0).</summary>
    public long MouseExtra;
}

// ----------------------------------------------------------------
// Image / CBG alignment
// ----------------------------------------------------------------
internal enum ImageAlign { Left = 0, Center = 1, Right = 2 }
