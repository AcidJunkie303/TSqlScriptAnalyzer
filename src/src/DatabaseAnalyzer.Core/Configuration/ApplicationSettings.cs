using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;

namespace DatabaseAnalyzer.Core.Configuration;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Setters are used during deserialization")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated during deserialization")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global", Justification = "Used during deserialization")]
internal sealed class ApplicationSettingsRaw
{
    public string? DefaultSchemaName { get; set; }
    public ScriptSourceSettingsRaw? ScriptSource { get; set; }
    public DiagnosticsSettingsRaw? Diagnostics { get; set; }

    public ApplicationSettings ToSettings() => new
    (
        Guard.Against.NullOrWhiteSpace(DefaultSchemaName),
        Guard.Against.Null(ScriptSource).ToSettings(),
        Guard.Against.Null(Diagnostics).ToSettings()
    );
}

public sealed record ApplicationSettings(
    string DefaultSchemaName,
    ScriptSourceSettings ScriptSource,
    DiagnosticsSettings Diagnostics
);
