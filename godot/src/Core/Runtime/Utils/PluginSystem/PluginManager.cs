using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using GameProcess = MinorShift.Emuera.GameProc.Process;

namespace MinorShift.Emuera.Runtime.Utils.PluginSystem;

internal class PluginManager
{
    private static PluginManager _instance;
    private readonly Dictionary<string, IPluginMethod> _methods = new();

    private PluginManager()
    {
        RegisterBuiltInMethods();
    }

    public static PluginManager GetInstance()
    {
        _instance ??= new PluginManager();
        return _instance;
    }

    public void SetParent(GameProcess process, ProcessState processState, object expressionMediator) { }

    public void LoadPlugins()
    {
        ClearMethods();
        RegisterBuiltInMethods();

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

    private void RegisterBuiltInMethods()
    {
        // Provide a minimal CALLSHARP compatibility layer even when no plugin DLL is present.
        AddMethod(new DelegatePluginMethod(
            "LAUNCH_BROWSER",
            "Open URL or local file with OS default handler",
            args =>
            {
                if (args == null || args.Length == 0 || !args[0].isString)
                    return;

                string target = args[0].strValue;
                if (string.IsNullOrWhiteSpace(target))
                    return;

                try
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = target,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Ignore launch failures to keep script execution stable.
                }
            }));

        // Optional LLM hooks used by some scripts. Keep as no-op until full integration is implemented.
        AddMethod(new DelegatePluginMethod("CALL_GEMINI", "No-op placeholder for LLM plugin method", _ => { }));
        AddMethod(new DelegatePluginMethod("CALL_OLLAMA", "No-op placeholder for LLM plugin method", _ => { }));
        AddMethod(new DelegatePluginMethod("CALL_GENERIC_LLM_API", "No-op placeholder for LLM plugin method", _ => { }));
    }

    private static string NormalizeKey(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        return Config.Config.IgnoreCase ? name.ToUpperInvariant() : name;
    }

    private sealed class DelegatePluginMethod : IPluginMethod
    {
        private readonly Action<PluginMethodParameter[]> _action;

        public DelegatePluginMethod(string name, string description, Action<PluginMethodParameter[]> action)
        {
            Name = name;
            Description = description;
            _action = action;
        }

        public string Name { get; }
        public string Description { get; }

        public void Execute(PluginMethodParameter[] args)
        {
            _action?.Invoke(args ?? Array.Empty<PluginMethodParameter>());
        }
    }
}
