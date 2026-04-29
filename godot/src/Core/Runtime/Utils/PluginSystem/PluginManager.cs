using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem;

internal class PluginManager
{
    private static PluginManager _instance;
    private readonly Dictionary<string, IPluginMethod> _methods = new();

    public static PluginManager GetInstance()
    {
        _instance ??= new PluginManager();
        return _instance;
    }

    public void SetParent(Process process, ProcessState processState, object expressionMediator) { }

    public void LoadPlugins()
    {
        ClearMethods();

        // DLL dynamic loading is supported only for desktop Windows build.
        if (!OperatingSystem.IsWindows())
            return;

        string pluginDir = Path.Combine(Program.ExeDir, "Plugins");
        if (!Directory.Exists(pluginDir))
            return;

        foreach (string pluginPath in Directory.EnumerateFiles(pluginDir, "*.dll"))
        {
            try
            {
                Assembly dll = Assembly.LoadFrom(pluginPath);
                Type manifestType = dll.GetTypes().FirstOrDefault(type => type.Name == "PluginManifest");
                if (manifestType == null)
                    continue;

                object manifest = Activator.CreateInstance(manifestType);
                if (manifest == null)
                    continue;

                MethodInfo getPluginMethods = manifestType.GetMethod("GetPluginMethods", BindingFlags.Public | BindingFlags.Instance);
                if (getPluginMethods == null || getPluginMethods.GetParameters().Length != 0)
                    continue;

                object methods = getPluginMethods.Invoke(manifest, null);
                if (methods is not IEnumerable enumerable)
                    continue;

                foreach (object method in enumerable)
                {
                    if (method is IPluginMethod pluginMethod)
                        AddMethod(pluginMethod);
                }
            }
            catch
            {
                // Keep boot process alive even when a plugin DLL fails to load.
            }
        }
    }

    public bool HasMethod(string name)
    {
        string key = NormalizeKey(name);
        return _methods.ContainsKey(key);
    }

    public IPluginMethod GetMethod(string name)
    {
        string key = NormalizeKey(name);
        return _methods.TryGetValue(key, out IPluginMethod method) ? method : null;
    }

    public IReadOnlyList<IPluginMethod> GetPluginMethods() => _methods.Values.ToList();

    private void AddMethod(IPluginMethod method)
    {
        if (method == null || string.IsNullOrWhiteSpace(method.Name))
            return;

        string key = NormalizeKey(method.Name);
        _methods[key] = method;
    }

    private void ClearMethods() => _methods.Clear();

    private static string NormalizeKey(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        return Config.Config.IgnoreCase ? name.ToUpperInvariant() : name;
    }
}
