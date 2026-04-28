// Gemuera stub: replaces Windows-only Sound.WMP.cs and Sound.NAudio.cs
// Actual audio playback is handled by Godot's AudioStreamPlayer via IGameConsole.
using System;

namespace MinorShift.Emuera.Runtime.Utils;

/// <summary>Stub sound manager — delegates to IGameConsole for actual playback.</summary>
internal static class SoundManager
{
    private static Bridge.IGameConsole _console;

    public static void Initialize(Bridge.IGameConsole console)
    {
        _console = console;
    }

    public static void PlayBgm(string path)
    {
        _console?.PlayBgm(path);
    }

    public static void StopBgm()
    {
        _console?.StopBgm();
    }

    public static void PlaySound(string path)
    {
        _console?.PlaySound(path);
    }

    public static void Dispose() { }
}
