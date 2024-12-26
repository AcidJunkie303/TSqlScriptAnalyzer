using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

continue here...

public sealed class Aj5017SettingsRaw : IRawSettings<Aj5017Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<MissingIndexSuppressionSettingsRaw>? MissingIndexSuppressions { get; set; }

    public Aj5017Settings ToSettings() => new
    (
        (MissingIndexSuppressions ?? [])
        .Select(a => a.ToSettings())
        .ToImmutableArray()
    );
}

public sealed record Aj5017Settings(
    IReadOnlyList<MissingIndexSuppressionSettings> MissingIndexSuppressions
) : ISettings<Aj5017Settings>
{
    public static Aj5017Settings Default { get; } = new Aj5017SettingsRaw
    {
        MissingIndexSuppressions =
        [
            new MissingIndexSuppressionSettingsRaw { FullColumnNamePattern = "*.sys.*", SuppressionReason = "Built-in schema" }
        ]
    }.ToSettings();

    public static string DiagnosticId => "Aj5017";
}
