using System.Text.Json.Serialization;

namespace MinorShift.Emuera.Runtime.Config.JSON;

/// <summary>JSON-backed configuration data for Emuera extended settings.</summary>
sealed class JSONConfigData
{
    /// <summary>Highlight button background on cursor hover.</summary>
    [JsonPropertyName("UseButtonFocusBackgroundColor")]
    public bool UseButtonFocusBackgroundColor { get; set; }

    /// <summary>Use the new PRNG implementation.</summary>
    [JsonPropertyName("UseNewRandom")]
    public bool UseNewRandom { get; set; }

    /// <summary>Enable scoped variable instructions.</summary>
    public bool UseScopedVariableInstruction { get; set; }
}
