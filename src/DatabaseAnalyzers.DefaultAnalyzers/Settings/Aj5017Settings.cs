using System.Collections.Immutable;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5017")]
internal sealed class Aj5017SettingsRaw : IRawDiagnosticSettings<Aj5017Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    [Description("List of of suppressed missing index warnings. For 'FullColumnNamePattern', Wildcards like `*` and `?` are supported.")]
    public IReadOnlyList<Aj5015SettingsSettingsEntryRaw>? MissingIndexOnForeignKeyColumnSuppressions { get; set; }

    public Aj5017Settings ToSettings()
        => MissingIndexOnForeignKeyColumnSuppressions is null
            ? Aj5017Settings.Default
            : new Aj5017Settings(MissingIndexOnForeignKeyColumnSuppressions.Select(static a => a.ToSettings()).ToImmutableArray());
}

public sealed record Aj5017Settings(
    IReadOnlyList<Aj5015SettingsSettingsEntry> MissingIndexOnForeignKeyColumnSuppressions
) : IDiagnosticSettings<Aj5017Settings>
{
    public static Aj5017Settings Default { get; } = new Aj5017SettingsRaw
    {
        MissingIndexOnForeignKeyColumnSuppressions =
        [
            new Aj5015SettingsSettingsEntryRaw { FullColumnNamePattern = "*.sys.*", SuppressionReason = "Built-in schema" }
        ]
    }.ToSettings();

    public static string DiagnosticId => "AJ5017";
}
