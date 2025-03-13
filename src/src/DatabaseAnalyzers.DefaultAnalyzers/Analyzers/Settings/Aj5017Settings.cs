using System.Collections.Immutable;
using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5017SettingsRaw : IRawSettings<Aj5017Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<Aj5015SettingsSettingsEntryRaw>? MissingIndexOnForeignKeyColumnSuppressions { get; set; }

    public Aj5017Settings ToSettings()
        => MissingIndexOnForeignKeyColumnSuppressions is null
            ? Aj5017Settings.Default
            : new Aj5017Settings(MissingIndexOnForeignKeyColumnSuppressions.Select(static a => a.ToSettings()).ToImmutableArray());
}

internal sealed record Aj5017Settings(
    [property: Description("List of of suppressed missing index warnings. For 'FullColumnNamePattern', Wildcards like `*` and `?` are supported.")]
    IReadOnlyList<Aj5015SettingsSettingsEntry> MissingIndexOnForeignKeyColumnSuppressions
) : ISettings<Aj5017Settings>
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
