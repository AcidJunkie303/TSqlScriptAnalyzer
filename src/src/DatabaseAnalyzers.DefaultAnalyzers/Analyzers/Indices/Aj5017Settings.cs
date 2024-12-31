using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
public sealed class Aj5017SettingsRaw : IRawSettings<Aj5017Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<MissingIndexSuppressionSettingsRaw>? MissingIndexOnForeignKeyColumnSuppressions { get; set; }

    public Aj5017Settings ToSettings()
        => MissingIndexOnForeignKeyColumnSuppressions is null
            ? Aj5017Settings.Default
            : new Aj5017Settings(MissingIndexOnForeignKeyColumnSuppressions.Select(static a => a.ToSettings()).ToImmutableArray());
}

public sealed record Aj5017Settings(
    IReadOnlyList<MissingIndexSuppressionSettings> MissingIndexOnForeignKeyColumnSuppressions
) : ISettings<Aj5017Settings>
{
    public static Aj5017Settings Default { get; } = new Aj5017SettingsRaw
    {
        MissingIndexOnForeignKeyColumnSuppressions =
        [
            new MissingIndexSuppressionSettingsRaw { FullColumnNamePattern = "*.sys.*", SuppressionReason = "Built-in schema" }
        ]
    }.ToSettings();

    public static string DiagnosticId => "AJ5017";
}
