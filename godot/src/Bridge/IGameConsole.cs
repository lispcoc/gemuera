using MinorShift.Emuera.Runtime;
using System.Threading.Tasks;

// Bridge layer between the ERA interpreter core and the Godot UI.
// The interpreter (Process) only calls methods on IGameConsole;
// the concrete implementation (GodotConsole) is in src/UI/.
namespace Gemuera.Bridge;

/// <summary>
/// Represents the abstract console that the ERA interpreter writes to and reads from.
/// Implemented by GodotConsole for Godot/Android/Web targets.
/// All methods are expected to be called from the interpreter thread;
/// implementations must ensure thread-safe dispatch to the UI thread.
/// </summary>
public interface IGameConsole
{
    // ----------------------------------------------------------------
    // Output
    // ----------------------------------------------------------------

    /// <summary>Print a styled text span. Does not add a newline.</summary>
    void PrintString(string text, StringStyle style);

    /// <summary>Flush the current line and start a new one.</summary>
    void PrintNewLine();

    /// <summary>DRAWLINE — draw a horizontal rule using a repeated character.</summary>
    void PrintLine(string lineChar);

    /// <summary>CLEARLINE n — remove the last n lines from the display.</summary>
    void ClearLine(int count);

    /// <summary>CLEARSCREEN / CLS — clear all displayed lines.</summary>
    void ClearScreen();

    /// <summary>HTML_PRINT — output a line of HTML-subset markup.
    /// opt=false (default): flush pending inline content, render as a standalone display block.
    /// opt=true: render inline (toPrintBuffer mode), no forced trailing newline.</summary>
    void PrintHtml(string html, bool opt = false);

    /// <summary>Render an absolutely positioned HTML div overlay.
    /// Created from <div rect='x,y,w,h'> elements extracted from HTML_PRINT content.
    /// innerHtml is the raw HTML content inside the div.</summary>
    void PrintHtmlDiv(string innerHtml, int x, int y, int width, int height, int depth, string bcolor, int borderPx, int paddingPx);

    /// <summary>PRINT_IMG / GCREATE — display an image inline.</summary>
    void PrintImage(string resourcePath, int width, int height, int align);

    /// <summary>PRINTBUTTON / PRINTBUTTONC — print a clickable button string.</summary>
    void PrintButton(string displayText, string inputValue, StringStyle style);

    /// <summary>
    /// Set the foreground color for subsequent Print calls.
    /// color is packed ARGB (same as System.Drawing.Color.ToArgb()).
    /// </summary>
    void SetForeColor(int argb);

    /// <summary>Set the background color for subsequent Print calls.</summary>
    void SetBackColor(int argb);

    /// <summary>Reset colors to the configured defaults.</summary>
    void ResetColors();

    // ----------------------------------------------------------------
    // Client Background (CBG)
    // ----------------------------------------------------------------

    /// <summary>CBGSETG — set a client background image.</summary>
    void CbgSet(string resourcePath, int x, int y, int width, int height);

    /// <summary>CBGCLEAR — remove all client background images.</summary>
    void CbgClear();

    /// <summary>CBGSETBMAPG — set the CBG button-map image used by INPUTMOUSEKEY hit testing.</summary>
    void SetCbgButtonMap(string resourcePath, int width, int height);

    /// <summary>CBGREMOVEBMAP — clear the active CBG button-map image.</summary>
    void ClearCbgButtonMap();

    // ----------------------------------------------------------------
    // Sound
    // ----------------------------------------------------------------

    /// <summary>PLAYBGM — start looping BGM.</summary>
    void PlayBgm(string resourcePath);

    /// <summary>STOPBGM — stop the BGM.</summary>
    void StopBgm();

    /// <summary>Fade out the BGM volume over the given duration (ms), then stop.</summary>
    void FadeBgm(int durationMs);

    /// <summary>PLAYSOUND — play a sound effect.</summary>
    void PlaySound(string resourcePath);

    /// <summary>Set BGM volume (0–100).</summary>
    void SetBgmVolume(int volume);

    /// <summary>Set SE volume (0–100).</summary>
    void SetSeVolume(int volume);

    /// <summary>Whether looping BGM is currently playing.</summary>
    bool IsBgmPlaying();

    /// <summary>Whether a sound effect is currently playing.</summary>
    bool IsSePlaying();

    // ----------------------------------------------------------------
    // Mouse
    // ----------------------------------------------------------------

    /// <summary>Get the current mouse position within the console area.</summary>
    (int X, int Y) GetMousePosition();

    // ----------------------------------------------------------------
    // Input (async — interpreter thread awaits result)
    // ----------------------------------------------------------------

    /// <summary>
    /// Request input from the user.  The interpreter thread awaits this task.
    /// The returned InputResult carries the raw string value entered, or null
    /// if the input timed out or was cancelled.
    /// </summary>
    Task<InputResult> RequestInputAsync(InputRequest request);

    // ----------------------------------------------------------------
    // Title / Status bar
    // ----------------------------------------------------------------

    /// <summary>TITLE — set the window / app title.</summary>
    void SetWindowTitle(string title);

    /// <summary>Get the current window/app title.</summary>
    string GetWindowTitle();

    /// <summary>Set the status bar text (bottom bar).</summary>
    void SetStatusBar(string text);

    // ----------------------------------------------------------------
    // State / Lifecycle
    // ----------------------------------------------------------------

    /// <summary>Whether the interpreter should keep running.</summary>
    bool IsRunning { get; }

    /// <summary>REDRAW — force an immediate display refresh.</summary>
    void DoRedraw();

    /// <summary>QUIT — signal that the interpreter wants to exit.</summary>
    void Quit();

    /// <summary>Show a fatal error and halt.</summary>
    void FatalError(string message);

    /// <summary>DEBUGPRINT — output a diagnostic line (dev builds only).</summary>
    void DebugPrint(string text);
}
