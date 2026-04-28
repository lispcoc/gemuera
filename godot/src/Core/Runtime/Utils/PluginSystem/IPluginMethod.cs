// IPluginMethod.cs - Stub port of the plugin interface.
// Plugin loading via Assembly.LoadFrom() is not supported on Android/Web.
namespace MinorShift.Emuera.Runtime.Utils.PluginSystem
{
    /// <summary>Argument type for a plugin method call.</summary>
    public sealed class PluginMethodParameter
    {
        public string Name  { get; set; }
        public object Value { get; set; }
        public string strValue => Value as string ?? string.Empty;
        public long   intValue => Value is long l ? l : 0;
    }

    /// <summary>Interface implemented by ERA plugin methods (CALLSHARP target).</summary>
    public interface IPluginMethod
    {
        string Name        { get; }
        string Description { get; }
        void   Execute(PluginMethodParameter[] args);
    }

    /// <summary>Stub: converts an expression term to a plugin method parameter.</summary>
    public static class PluginMethodParameterBuilder
    {
        public static PluginMethodParameter ConvertTerm(object term, object exm)
            => new PluginMethodParameter { Name = "", Value = null };
    }
}
