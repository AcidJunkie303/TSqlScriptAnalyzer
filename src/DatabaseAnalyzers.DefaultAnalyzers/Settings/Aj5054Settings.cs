using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5054")]
internal sealed class Aj5054SettingsRaw : IRawDiagnosticSettings<Aj5054Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    [Description("Database names to ignore.")]
    public IReadOnlyCollection<string?>? DatabasesToExclude { get; set; }

    [Description("Column names to ignore.")]
    public IReadOnlyCollection<string?>? ColumnNamesToExclude { get; set; }

    public Aj5054Settings ToSettings() => new
    (
        ToFrozenSet(DatabasesToExclude),
        ToFrozenSet(ColumnNamesToExclude)
    );

    private static FrozenSet<string> ToFrozenSet(IReadOnlyCollection<string?>? values)
        => values
               ?.WhereNotNullOrWhiteSpaceOnly()
               .ToFrozenSet(StringComparer.OrdinalIgnoreCase)
           ?? FrozenSet<string>.Empty;
}

public sealed record Aj5054Settings(
    FrozenSet<string> DatabasesToExclude,
    FrozenSet<string> ColumnNamesToExclude
) : IDiagnosticSettings<Aj5054Settings>
{
    public static string DiagnosticId => "AJ5054";

    public static Aj5054Settings Default { get; } = new Aj5054SettingsRaw
    {
        DatabasesToExclude = [],
        ColumnNamesToExclude = ["id"]
    }.ToSettings();
}
