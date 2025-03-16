using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5054")]
internal sealed class Aj5054SettingsRaw : IRawSettings<Aj5054Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyCollection<string?>? DatabasesToExclude { get; set; }
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
    [property: Description("Database names to ignore.")]
    FrozenSet<string> DatabasesToExclude,
    [property: Description("Column names to ignore.")]
    FrozenSet<string> ColumnNamesToExclude
) : ISettings<Aj5054Settings>
{
    public static string DiagnosticId => "AJ5054";

    public static Aj5054Settings Default { get; } = new Aj5054SettingsRaw
    {
        DatabasesToExclude = [],
        ColumnNamesToExclude = ["id"]
    }.ToSettings();
}
