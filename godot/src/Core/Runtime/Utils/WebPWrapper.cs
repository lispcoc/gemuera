// Gemuera stub: replaces WebPWrapper.cs (libwebp P/Invoke)
// Godot 4 has built-in WebP support via Image.LoadWebpFromBuffer().
// Actual WebP loading is handled by the Godot UI layer.
using System;

namespace MinorShift.Emuera.Runtime.Utils;

internal static class WebPWrapper
{
    /// <summary>Returns true — WebP is supported via Godot's built-in decoder.</summary>
    public static bool IsAvailable => true;

    /// <summary>Not used in Godot port. Image loading is done via IGameConsole.</summary>
    public static byte[] Decode(byte[] webpData)
    {
        throw new NotSupportedException("WebP decoding must be done through the Godot UI layer.");
    }
}
