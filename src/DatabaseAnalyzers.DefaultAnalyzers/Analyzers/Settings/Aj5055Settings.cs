using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5055")]
internal sealed class Aj5055SettingsRaw : IRawDiagnosticSettings<Aj5055Settings>
{
    public IReadOnlyCollection<string?>? ExcludedDatabaseNames { get; set; }

    public Aj5055Settings ToSettings() => new
    (
        ExcludedDatabaseNames
            ?.WhereNotNullOrWhiteSpaceOnly()
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase)
        ?? FrozenSet<string>.Empty
    );
}

public sealed record Aj5055Settings(
    [property: Description("Database names to exclude from this rule.")]
    FrozenSet<string> ExcludedDatabaseNames
) : IDiagnosticSettings<Aj5055Settings>
{
    public static Aj5055Settings Default { get; } = new(FrozenSet<string>.Empty);

    public static string DiagnosticId => "AJ5055";
}
