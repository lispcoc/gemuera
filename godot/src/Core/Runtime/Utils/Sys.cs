// Gemuera adaptation: Sys.cs — removes Windows Forms dependency
using System;
using System.IO;

namespace MinorShift.Emuera.Runtime.Utils;

public static class AssemblyData
{
    static AssemblyData()
    {
        ExePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        WorkingDir = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
        ExeDir = (Path.GetDirectoryName(ExePath) ?? WorkingDir) + Path.DirectorySeparatorChar;
        ExeName = Path.GetFileName(ExePath) ?? "Gemuera";
        emueraVer = typeof(AssemblyData).Assembly.GetName().Version;
        EmueraVersionText = "Gemuera " + (emueraVer?.ToString() ?? "0.1");
    }

    public static readonly string ExePath;
    public static readonly Version emueraVer;
    public static readonly string EmueraVersionText;
    /// <summary>Executable directory with trailing separator.</summary>
    public static readonly string ExeDir;
    public static readonly string WorkingDir;
    public static readonly string ExeName;

    /// <summary>Multi-instance check — always returns false on mobile/web.</summary>
    public static bool PrevInstance()
    {
#if GODOT
        return false; // Single-app model on mobile/web
#else
        try
        {
            string name = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return System.Diagnostics.Process.GetProcessesByName(name).Length > 1;
        }
        catch { return false; }
#endif
    }
}
