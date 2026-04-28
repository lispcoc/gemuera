// Gemuera stub: replaces WinInput.cs (Win32 keyboard/mouse simulation P/Invoke)
// Input simulation is not needed in the Godot port.
namespace MinorShift.Emuera.Runtime.Utils;

internal static class WinInput
{
    public static void SendKey(int keyCode) { /* no-op in Godot port */ }
    public static void SendString(string text) { /* no-op */ }
}
