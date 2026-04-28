// Sound.cs - Stub replacement for Sound.WMP.cs and Sound.NAudio.cs.
// Actual audio playback is handled by Godot's AudioStreamPlayer via IGameConsole.
namespace MinorShift.Emuera.Runtime.Utils;

/// <summary>
/// Stub Sound object — delegates playback to SoundManager which in turn calls IGameConsole.
/// </summary>
internal class Sound
{
    private string _path;
    private int _repeat;

    public Sound() { }

    public void play(string filename, int repeat = 1)
    {
        _path   = filename;
        _repeat = repeat;
        if (repeat == -1)
            SoundManager.PlayBgm(filename);
        else
            SoundManager.PlaySound(filename);
    }

    public void stop()
    {
        SoundManager.StopBgm();
    }

    public bool isPlaying() => false; // stub

    public void setVolume(int vol) { } // stub
}
