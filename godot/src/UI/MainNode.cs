// Gemuera — Main entry point Node
// Attach this script to the root Node of Main.tscn
using Godot;
using Gemuera.Bridge;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Gemuera.UI;

public partial class MainNode : Node
{
    // ----------------------------------------------------------------
    // Exported paths — set these in the Godot inspector or via ProjectSettings
    // ----------------------------------------------------------------

    /// <summary>
    /// Absolute or user:// path to the ERA game root folder.
    /// On Android this would typically be under user://
    /// On Web it would be a path to files bundled in the .pck
    /// </summary>
    [Export] public string GameRootDir = "user://game";

    [Export] public bool DebugMode = false;

    /// <summary>Reference to the ConsoleNode child (Console.tscn instance).</summary>
    [Export] public NodePath ConsolePath = "Console";

    // ----------------------------------------------------------------
    // Runtime fields
    // ----------------------------------------------------------------

    private ConsoleNode _console;
    private MinorShift.Emuera.GameProc.Process _process;
    private bool _started = false;

    // ----------------------------------------------------------------
    // Godot lifecycle
    // ----------------------------------------------------------------

    public override void _Ready()
    {
        // Determine log file path: next to the project, or user data dir as fallback
        string logDir = ProjectSettings.GlobalizePath("user://");
        string logPath = Path.Combine(logDir, "gemuera_runtime.log");

        // Also write a copy next to repo root for easy access during development
        string devLogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "gemuera_runtime.log");

        try { devLogPath = Path.GetFullPath(devLogPath); } catch { }

        GemueraLogger.Init(File.Exists(logDir) || Directory.Exists(logDir)
            ? logPath : devLogPath);

        // Hook unhandled C# exceptions (background threads)
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            string msg = ex?.ToString() ?? args.ExceptionObject?.ToString() ?? "Unknown";
            GemueraLogger.LogError($"[UnhandledException] {msg}");
            GD.PrintErr($"[Gemuera] UnhandledException: {msg}");
        };

        _console = GetNode<ConsoleNode>(ConsolePath);

        // Resolve game root path via Godot's path system
        string resolvedRoot = ProjectSettings.GlobalizePath(GameRootDir);
        GD.Print($"[Gemuera] Game root: {resolvedRoot}");
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
            GemueraLogger.Log($"Paths set. ExeDir={Program.ExeDir}  ErbDir={Program.ErbDir}");

            // 2. Load config
            GemueraLogger.Log("Loading config...");
            MinorShift.Emuera.Runtime.Config.ConfigData.Instance.LoadConfig();
            MinorShift.Emuera.Runtime.Config.Config.SetConfig(
                MinorShift.Emuera.Runtime.Config.ConfigData.Instance);
            GemueraLogger.Log("Config loaded.");

            // 3. Create and initialise the interpreter (EmueraConsole is created inside Process)
            GemueraLogger.Log("Creating Process...");
            _process = new MinorShift.Emuera.GameProc.Process(_console);
            GlobalStatic.Process  = _process;
            GlobalStatic.Console  = _process.Console;  // the EmueraConsole adapter

            GemueraLogger.Log("Calling Process.Initialize...");
            // Pass a StreamWriter so Process.Initialize reports each sub-step
            using var logWriter = new System.IO.StreamWriter(
                System.IO.Path.Combine(
                    ProjectSettings.GlobalizePath("user://"),
                    "gemuera_init.log"),
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
}
