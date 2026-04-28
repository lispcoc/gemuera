// Gemuera — Main entry point Node
// Attach this script to the root Node of Main.tscn
using Godot;
using Gemuera.Bridge;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
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
        _console = GetNode<ConsoleNode>(ConsolePath);

        // Resolve game root path via Godot's path system
        string resolvedRoot = ProjectSettings.GlobalizePath(GameRootDir);
        GD.Print($"[Gemuera] Game root: {resolvedRoot}");

        // Boot the interpreter async so the UI remains responsive
        _ = StartInterpreterAsync(resolvedRoot);
    }

    private async Task StartInterpreterAsync(string gameRoot)
    {
        // 1. Initialise static path/encoding settings
        Program.SetPaths(gameRoot, DebugMode);

        // 2. Load config
        MinorShift.Emuera.Runtime.Config.ConfigData.Instance.LoadConfig();
        MinorShift.Emuera.Runtime.Config.Config.SetConfig(
            MinorShift.Emuera.Runtime.Config.ConfigData.Instance);

        // 3. Create and initialise the interpreter (EmueraConsole is created inside Process)
        _process = new MinorShift.Emuera.GameProc.Process(_console);
        GlobalStatic.Process  = _process;
        GlobalStatic.Console  = _process.Console;  // the EmueraConsole adapter

        bool ok = await _process.Initialize(null);
        if (!ok)
        {
            GD.PrintErr("[Gemuera] Interpreter failed to initialise.");
            _console.FatalError("ERBスクリプトの初期化に失敗しました。\nコンソールを確認してください。");
            return;
        }

        GD.Print("[Gemuera] Interpreter ready. Starting game loop.");
        _started = true;

        // 5. Run the ERA game loop on a background thread
        await Task.Run(() => _process.Run());

        GD.Print("[Gemuera] Game loop ended.");
    }

    public override void _ExitTree()
    {
        // Signal the interpreter to stop if it's running
        if (_process != null)
        {
            try { _process.RequestQuit(); }
            catch { /* ignore */ }
        }
    }
}
