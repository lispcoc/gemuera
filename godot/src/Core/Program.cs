// Gemuera adaptation of Program.cs
// Replaces WinForms entrypoint with cross-platform static config class.
// The actual Godot entrypoint is in src/UI/MainNode.cs (Godot Node).
using MinorShift.Emuera.Runtime.Config;
using System;
using System.IO;

namespace MinorShift.Emuera;

/// <summary>
/// Holds global path constants initialised by the Godot UI layer before
/// the interpreter is started.  Replaces the original WinForms Program class.
/// </summary>
internal static class Program
{
    /// <summary>Base directory containing the ERA game files.</summary>
    public static string ExeDir  { get; private set; } = "";
    /// <summary>Directory containing CSV data files.</summary>
    public static string CsvDir  { get; private set; } = "";
    /// <summary>Directory containing ERB script files.</summary>
    public static string ErbDir  { get; private set; } = "";
    /// <summary>Directory for save files.</summary>
    public static string SavDir  { get; private set; } = "";
    /// <summary>Debug config directory.</summary>
    public static string DebugDir { get; private set; } = "";

    /// <summary>Analysis mode (no game run, only script validation).</summary>
    public static bool AnalysisMode { get; private set; } = false;

    /// <summary>Debug mode flag.</summary>
    public static bool DebugMode { get; private set; } = false;

    /// <summary>Content (resources) directory.</summary>
    public static string ContentDir { get; private set; } = "";
    public static string SoundDir { get; private set; } = "";

    /// <summary>Variable data directory (for KDMM-format .dat saves).</summary>
    public static string DatDir { get; private set; } = "";

    /// <summary>Reboot flag — set by QUIT_AND_RESTART instruction.</summary>
    public static bool rebootFlag;

    /// <summary>Files to analyse in AnalysisMode (null outside analysis mode).</summary>
    public static System.Collections.Generic.List<string> AnalysisFiles;

    /// <summary>Override the save directory (used on Android/Web for user://).</summary>
    public static void SetSavDir(string savDir)
    {
        SavDir = savDir;
    }

    /// <summary>
    /// Must be called by the Godot UI layer before creating Process.
    /// </summary>
    public static void SetPaths(string gameRootDir, bool debugMode = false, bool analysisMode = false)
    {
        // Normalise: ensure trailing separator
        string root = gameRootDir.TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
        ExeDir    = root;
        CsvDir    = root + "csv"  + Path.DirectorySeparatorChar;
        ErbDir    = root + "erb"  + Path.DirectorySeparatorChar;
        SavDir      = root + "sav"   + Path.DirectorySeparatorChar;
        DebugDir    = root + "debug" + Path.DirectorySeparatorChar;
        ContentDir  = root + "resources" + Path.DirectorySeparatorChar;
        DatDir      = root + "dat"       + Path.DirectorySeparatorChar;
        SoundDir    = root + "sound"     + Path.DirectorySeparatorChar;
        DebugMode   = debugMode;
        AnalysisMode = analysisMode;

        // Register Shift-JIS encoding provider (needed for legacy ERA files)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        System.Globalization.CultureInfo.CurrentCulture =
            System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture =
            System.Globalization.CultureInfo.InvariantCulture;
    }
}
