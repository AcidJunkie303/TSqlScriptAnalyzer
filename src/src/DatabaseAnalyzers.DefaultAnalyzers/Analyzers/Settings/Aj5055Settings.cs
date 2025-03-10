using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5055SettingsRaw : IRawSettings<Aj5055Settings>
{
    public IReadOnlyCollection<string?>? ExcludedDatabaseNames { get; set; }

    public Aj5055Settings ToSettings() => new
    (
        ExcludedDatabaseNames
            ?.WhereNotNullOrWhiteSpaceOnly()
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase)
        ?? []
    );
}

internal sealed record Aj5055Settings(
    [property: Description("Database names to exclude from this rule.")]
    FrozenSet<string> ExcludedDatabaseNames
) : ISettings<Aj5055Settings>
{
    public static Aj5055Settings Default { get; } = new([]);

    public static string DiagnosticId => "AJ5055";
}
