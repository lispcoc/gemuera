// Gemuera stub: PluginManager — plugin loading via Assembly.LoadFrom() is not
// supported on Android/Web.  On desktop this could be re-enabled.
using System.Collections.Generic;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem;

internal static class PluginManager
{
    /// <summary>
    /// Stub: returns an empty list.
    /// On desktop builds, re-implement using Assembly.LoadFrom() if needed.
    /// </summary>
    public static IReadOnlyList<object> LoadPlugins(string pluginDir) =>
        System.Array.Empty<object>();
}
