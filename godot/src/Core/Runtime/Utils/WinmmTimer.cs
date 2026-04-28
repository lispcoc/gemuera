// Gemuera stub: replaces WinmmTimer.cs (Win32 timeGetTime P/Invoke)
using System;

namespace MinorShift.Emuera.Runtime.Utils;

/// <summary>
/// High-resolution millisecond timer stub.
/// On Godot we use Environment.TickCount64; on all platforms this is accurate enough.
/// </summary>
internal static class WinmmTimer
{
    public static uint timeGetTime()
    {
        return (uint)(Environment.TickCount64 & 0xFFFFFFFF);
    }
}
