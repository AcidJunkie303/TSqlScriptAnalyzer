using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;

namespace DatabaseAnalyzer.Core.Configuration;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Setters are used during deserialization")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated during deserialization")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global", Justification = "Used during deserialization")]
internal sealed class ApplicationSettingsRaw
{
    public string? DefaultSchemaName { get; set; }
    public PluginsSettingsRaw? Plugins { get; set; }
    public ScriptSourceSettingsRaw? ScriptSource { get; set; }
    public DiagnosticsSettingsRaw? Diagnostics { get; set; }

    public ApplicationSettings ToSettings() => new
    (
        DefaultSchemaName: Guard.Against.NullOrWhiteSpace(DefaultSchemaName),
        Plugins: Plugins?.ToSettings() ?? PluginsSettings.Default,
        ScriptSource: Guard.Against.Null(ScriptSource).ToSettings(),
        Diagnostics: Guard.Against.Null(Diagnostics).ToSettings()
    );
}

public sealed record ApplicationSettings(
    string DefaultSchemaName,
    PluginsSettings Plugins,
    ScriptSourceSettings ScriptSource,
    DiagnosticsSettings Diagnostics
);
