// Gemuera stub: PluginManager — plugin loading via Assembly.LoadFrom() is not
// supported on Android/Web.  On desktop this could be re-enabled.
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.Runtime.Script;
using System.Collections.Generic;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem;

internal class PluginManager
{
    private static PluginManager _instance;

    public static PluginManager GetInstance()
    {
        _instance ??= new PluginManager();
        return _instance;
    }

    public void SetParent(Process process, ProcessState processState, object expressionMediator) { }

    public void LoadPlugins() { }

    public bool HasMethod(string name) => false;

    public IPluginMethod GetMethod(string name) => null;

    public IReadOnlyList<IPluginMethod> GetPluginMethods() => System.Array.Empty<IPluginMethod>();
}
