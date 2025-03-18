using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5025")]
internal sealed class Aj5025SettingsRaw : IRawDiagnosticSettings<Aj5025Settings>
{
    public string? ExistenceCheckPatternForTableCreation { get; set; }

    public Aj5025Settings ToSettings() => new
    (
        ExistenceCheckPatternForTableCreation
    );
}

public sealed record Aj5025Settings(
    [property: Description("The statement pattern which must preceed table creation statements. The following insertion strings are supported: `{TableSchemaName}` and `{TableName}`.")]
    string? ExistenceCheckPatternForTableCreation
) : IDiagnosticSettings<Aj5025Settings>
{
    public static Aj5025Settings Default { get; } = new(ExistenceCheckPatternForTableCreation: null);
    public static string DiagnosticId => "AJ5025";
}
