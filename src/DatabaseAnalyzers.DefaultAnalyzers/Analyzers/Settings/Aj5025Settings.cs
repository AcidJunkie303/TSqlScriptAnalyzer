using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5025SettingsRaw : IRawSettings<Aj5025Settings>
{
    public string? ExistenceCheckPatternForTableCreation { get; set; }

    public Aj5025Settings ToSettings() => new
    (
        ExistenceCheckPatternForTableCreation
    );
}

internal sealed record Aj5025Settings(
    [property: Description("The statement pattern which must preceed table creation statements. The following insertion strings are supported: `{TableSchemaName}` and `{TableName}`.")]
    string? ExistenceCheckPatternForTableCreation
) : ISettings<Aj5025Settings>
{
    public static Aj5025Settings Default { get; } = new(ExistenceCheckPatternForTableCreation: null);
    public static string DiagnosticId => "AJ5025";
}
