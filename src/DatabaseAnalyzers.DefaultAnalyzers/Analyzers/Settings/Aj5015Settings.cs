using System.Collections.Immutable;
using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5015")]
internal sealed class Aj5015SettingsRaw : IRawDiagnosticSettings<Aj5015Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<Aj5015SettingsSettingsEntryRaw>? MissingIndexSuppressions { get; set; }

    public Aj5015Settings ToSettings()
        => MissingIndexSuppressions is null
            ? Aj5015Settings.Default
            : new Aj5015Settings(MissingIndexSuppressions.Select(static a => a.ToSettings()).ToImmutableArray());
}

public sealed record Aj5015Settings(
    [property: Description("List of of suppressed missing index warnings. For 'FullColumnNamePattern', Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Aj5015SettingsSettingsEntry> MissingIndexSuppressions
) : IDiagnosticSettings<Aj5015Settings>
{
    public static Aj5015Settings Default { get; } = new Aj5015SettingsRaw
    {
        MissingIndexSuppressions =
        [
            new Aj5015SettingsSettingsEntryRaw { FullColumnNamePattern = "*.sys.*", SuppressionReason = "Built-in schema" }
        ]
    }.ToSettings();

    public static string DiagnosticId => "AJ5015";
}
