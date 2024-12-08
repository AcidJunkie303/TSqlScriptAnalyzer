using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Ardalis.GuardClauses;
using DatabaseAnalyzer.Core.Extensions;

namespace DatabaseAnalyzer.Core.Configuration;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Setters are used during deserialization")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated during deserialization")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global", Justification = "Used during deserialization")]
internal sealed class ApplicationSettingsRaw
{
    public string? DatabaseToAnalyze { get; set; }
    public string? DefaultSchemaName { get; set; }
    public IReadOnlyCollection<string?>? AdditionalDatabasesToLoad { get; set; }
    public ScriptSourceSettingsRaw? ScriptSource { get; set; }
    public DiagnosticsSettingsRaw? Diagnostics { get; set; }

    public ApplicationSettings ToSettings() => new
    (
        DatabaseToAnalyze: Guard.Against.NullOrWhiteSpace(DatabaseToAnalyze),
        DefaultSchemaName: Guard.Against.NullOrWhiteSpace(DefaultSchemaName),
        AdditionalDatabasesToLoad: AdditionalDatabasesToLoad.EmptyIfNull().WhereNotNullOrWhiteSpace().TrimAllStrings().ToImmutableArray(),
        ScriptSource: Guard.Against.Null(ScriptSource).ToSettings(),
        Diagnostics: Guard.Against.Null(Diagnostics).ToSettings()
    );
}

public sealed record ApplicationSettings(
    string DatabaseToAnalyze,
    string DefaultSchemaName,
    ImmutableArray<string> AdditionalDatabasesToLoad,
    ScriptSourceSettings ScriptSource,
    DiagnosticsSettings Diagnostics
);
