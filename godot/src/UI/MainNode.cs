// Gemuera — Main entry point Node
// Attach this script to the root Node of Main.tscn
using Godot;
using Gemuera.Bridge;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GodotFileAccess = Godot.FileAccess;

namespace Gemuera.UI;

public partial class MainNode : Node
{
    // ----------------------------------------------------------------
    // Exported paths — set these in the Godot inspector or via ProjectSettings
    // ----------------------------------------------------------------

    /// <summary>
    /// Override path for the ERA game root folder.
    /// If empty (default), the directory containing the executable is used.
    /// On Android this would typically be under user://
    /// On Web it would be a path to files bundled in the .pck
    /// </summary>
    [Export] public string GameRootDir = "";

    [Export] public bool DebugMode = false;

    /// <summary>Reference to the ConsoleNode child (Console.tscn instance).</summary>
    [Export] public NodePath ConsolePath = "Console";

    // ----------------------------------------------------------------
    // Runtime fields
    // ----------------------------------------------------------------

    private ConsoleNode _console;
    private MinorShift.Emuera.GameProc.Process _process;
    private bool _started = false;
    private string _exeDir = "";

    // ----------------------------------------------------------------
    // Godot lifecycle
    // ----------------------------------------------------------------

    public override void _Ready()
    {
        // Determine exe directory — used for logs and game root
        _exeDir = Path.GetDirectoryName(OS.GetExecutablePath())
            ?? AppDomain.CurrentDomain.BaseDirectory;

        // Log files go next to the exe
        string logPath = Path.Combine(_exeDir, "gemuera_runtime.log");
        GemueraLogger.Init(logPath);

        // Hook unhandled C# exceptions (background threads)
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            string msg = ex?.ToString() ?? args.ExceptionObject?.ToString() ?? "Unknown";
            GemueraLogger.LogError($"[UnhandledException] {msg}");
            GD.PrintErr($"[Gemuera] UnhandledException: {msg}");
        };

        _console = GetNode<ConsoleNode>(ConsolePath);

        // Resolve game root path — use executable directory by default
        string resolvedRoot;
        if (!string.IsNullOrEmpty(GameRootDir))
            resolvedRoot = ProjectSettings.GlobalizePath(GameRootDir);
        else
            resolvedRoot = _exeDir;

        GD.Print($"[Gemuera] ExeDir: {_exeDir}");
        GD.Print($"[Gemuera] Game root: {resolvedRoot}");
        GemueraLogger.Log($"ExeDir: {_exeDir}");
        GemueraLogger.Log($"Game root resolved: {resolvedRoot}");
        GemueraLogger.Log($"Log file: {logPath}");

        // Boot the interpreter async so the UI remains responsive
        _ = StartInterpreterAsync(resolvedRoot);
    }

    private async Task StartInterpreterAsync(string gameRoot)
    {
        try
        {
            GemueraLogger.Log("StartInterpreterAsync begin");

            // 1. Initialise static path/encoding settings
            Program.SetPaths(gameRoot, DebugMode);

            // On Android and Web, save data must go to user:// (writable user data dir).
            // Override SavDir to a platform-appropriate writable location.
            string featureName = OS.GetName().ToLowerInvariant();
            bool isAndroid = featureName == "android";
            bool isWeb     = featureName == "web" || featureName == "html5";
            if (isAndroid || isWeb)
            {
                string savDir = ProjectSettings.GlobalizePath("user://sav") + Path.DirectorySeparatorChar;
                Directory.CreateDirectory(savDir);
                Program.SetSavDir(savDir);
                GemueraLogger.Log($"Platform={featureName}: SavDir redirected to user://sav -> {savDir}");
            }
            GemueraLogger.Log($"Paths set. ExeDir={Program.ExeDir}  ErbDir={Program.ErbDir}");

            // 2. Load config
            GemueraLogger.Log("Loading config...");
            MinorShift.Emuera.Runtime.Config.ConfigData.Instance.LoadConfig();
            MinorShift.Emuera.Runtime.Config.Config.SetConfig(
                MinorShift.Emuera.Runtime.Config.ConfigData.Instance);
            MinorShift.Emuera.Runtime.Config.JSON.JSONConfig.Load();
            GemueraLogger.Log("Config loaded.");

            // 3. Preload all ERB/CSV files into memory cache (mirrors EmueraConsole.StartConsole)
            GemueraLogger.Log("Preloading files...");
            MinorShift.Emuera.Runtime.Utils.Preload.Clear();
            await PreloadGodotDir(Program.ErbDir);
            await PreloadGodotDir(Program.CsvDir);
            GemueraLogger.Log("Preload done.");

            // 4. Create and initialise the interpreter (EmueraConsole is created inside Process)
            GemueraLogger.Log("Creating Process...");
            _process = new MinorShift.Emuera.GameProc.Process(_console);
            GlobalStatic.Process  = _process;
            GlobalStatic.Console  = _process.Console;  // the EmueraConsole adapter

            GemueraLogger.Log("Calling Process.Initialize...");
            // Pass a StreamWriter so Process.Initialize reports each sub-step
            using var logWriter = new System.IO.StreamWriter(
                Path.Combine(_exeDir, "gemuera_init.log"),
                append: false, System.Text.Encoding.UTF8);
            logWriter.AutoFlush = true;
            bool ok = await _process.Initialize(logWriter);
            logWriter.Flush();
            GemueraLogger.Log("Process.Initialize done. See gemuera_init.log for per-step details.");
            if (!ok)
            {
                string err = "Process.Initialize returned false — script loading failed.";
                GemueraLogger.LogError(err);
                GD.PrintErr($"[Gemuera] Interpreter failed to initialise.");
                _console.FatalError(
                    $"ゲームの初期化に失敗しました。\n\n" +
                    $"ERBスクリプトと CSV ファイルを\n{gameRoot}\\erb\\ および {gameRoot}\\csv\\ に配置してください。\n\n" +
                    $"(詳細は gemuera_init.log を参照)");
                return;
            }

            GemueraLogger.Log("Interpreter ready. Starting game loop.");
            GD.Print("[Gemuera] Interpreter ready. Starting game loop.");
            _started = true;

            // 4. Run the ERA game loop on a background thread
            await Task.Run(() =>
            {
                try
                {
                    GemueraLogger.Log("Process.Run() start");
                    _process.Run();
                    GemueraLogger.Log("Process.Run() returned normally");
                }
                catch (Exception ex)
                {
                    GemueraLogger.LogException("Process.Run", ex);
                    GD.PrintErr($"[Gemuera] Process.Run error: {ex}");
                    _console.FatalError($"ゲームループでエラーが発生しました:\n{ex.Message}");
                }
            });

            GemueraLogger.Log("Game loop ended.");
            GD.Print("[Gemuera] Game loop ended.");
        }
        catch (Exception ex)
        {
            GemueraLogger.LogException("StartInterpreterAsync", ex);
            GD.PrintErr($"[Gemuera] StartInterpreterAsync error: {ex}");
            _console?.FatalError($"初期化中にエラーが発生しました:\n{ex.Message}");
        }
    }

    public override void _ExitTree()
    {
        GemueraLogger.Log("_ExitTree called — shutting down interpreter.");
        // Signal the interpreter to stop if it's running
        if (_process != null)
        {
            try { _process.RequestQuit(); }
            catch (Exception ex) { GemueraLogger.LogException("RequestQuit", ex); }
        }
    }

    /// <summary>
    /// Load all ERA script/data files from a Godot directory path (res:// or user://).
    /// Uses Godot's DirAccess so it works on Android and Web where System.IO doesn't.
    /// Falls back to System.IO for regular filesystem paths.
    /// </summary>
    private static async Task PreloadGodotDir(string dirPath)
    {
        if (string.IsNullOrEmpty(dirPath)) return;

        // For regular file system paths, let Preload handle it normally
        if (!dirPath.StartsWith("res://") && !dirPath.StartsWith("user://"))
        {
            await MinorShift.Emuera.Runtime.Utils.Preload.Load(dirPath);
            return;
        }

        // Walk the directory recursively using Godot's DirAccess
        var toVisit = new Queue<string>();
        toVisit.Enqueue(dirPath.TrimEnd('/', '\\'));

        var gameFiles = new List<string>();
        var validExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".csv", ".erb", ".erh", ".erd", ".als" };

        while (toVisit.Count > 0)
        {
            string current = toVisit.Dequeue();
            using var da = DirAccess.Open(current);
            if (da == null) continue;
            da.ListDirBegin();
            string entry;
            while ((entry = da.GetNext()) != "")
            {
                if (entry == "." || entry == "..") continue;
                string fullPath = current + "/" + entry;
                if (da.CurrentIsDir())
                    toVisit.Enqueue(fullPath);
                else if (validExts.Contains(Path.GetExtension(entry)))
                    gameFiles.Add(fullPath);
            }
            da.ListDirEnd();
        }

        // Load each file into the Preload cache
        await Task.Run(() =>
        {
            foreach (string filePath in gameFiles)
            {
                using var fa = GodotFileAccess.Open(filePath, GodotFileAccess.ModeFlags.Read);
                if (fa == null) continue;
                string text = fa.GetAsText();
                string[] lines = text.Split('\n');
                // Strip trailing \r from each line (Windows line endings)
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = lines[i].TrimEnd('\r');
                MinorShift.Emuera.Runtime.Utils.Preload.AddToCache(filePath, lines);
            }
        });
    }
}
