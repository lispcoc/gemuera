// Gemuera adaptation of GlobalStatic.cs
// Removes PrivateFontCollection (System.Drawing.Text) — Godot manages fonts.
// EmueraConsole wraps IGameConsole; Process constructs it internally.
using MinorShift.Emuera.GameData.Variable;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Runtime.Script.Data;
using MinorShift.Emuera.Runtime.Script.Statements;
using MinorShift.Emuera.Runtime.Script.Statements.Variable;
using MinorShift.Emuera.Runtime.Utils;
using System.Collections.Generic;

namespace MinorShift.Emuera;

internal static class GlobalStatic
{
    // Initialisation order: from top (created first) to bottom (created last).
    // A field lower in the list may return null if accessed before creation.

    public static EmueraConsole Console;
    public static GameProc.Process Process;
    public static Runtime.Script.Data.GameBase GameBaseData;
    public static ConstantData ConstantData;
    public static VariableData VariableData;
    public static VariableEvaluator VEvaluator;
    public static IdentifierDictionary IdentifierDictionary;
    public static ExpressionMediator EMediator;
    public static LabelDictionary LabelDictionary;

    // Scratch dictionary for ERB analysis mode
    public static Dictionary<string, long> tempDic = new(System.StringComparer.OrdinalIgnoreCase);

    public static bool ForceQuitAndRestart;

    public static CtrlZ ctrlZ = new();

    // PrivateFontCollection stub — font loading is managed by Godot, not GDI+.
    public static System.Drawing.Text.PrivateFontCollection Pfc = new();

#if DEBUG
    public static System.Collections.Generic.List<Runtime.Script.Statements.FunctionLabelLine> StackList = [];
#endif

    public static void Reset()
    {
        Process = null;
        ConstantData = null;
        GameBaseData = null;
        EMediator = null;
        VEvaluator = null;
        VariableData = null;
        Console = null;
        LabelDictionary = null;
        IdentifierDictionary = null;
        tempDic.Clear();
    }
}
