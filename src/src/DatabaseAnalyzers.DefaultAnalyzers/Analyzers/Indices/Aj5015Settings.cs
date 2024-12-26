using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class Aj5015SettingsRaw : IRawSettings<Aj5015Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<MissingIndexSuppressionSettingsRaw>? MissingIndexSuppressions { get; set; }

    public Aj5015Settings ToSettings() => new
    (
        (MissingIndexSuppressions ?? [])
        .Select(a => a.ToSettings())
        .ToImmutableArray()
    );
}

public sealed record Aj5015Settings(
    IReadOnlyList<MissingIndexSuppressionSettings> MissingIndexSuppressions
) : ISettings<Aj5015Settings>
{
    public static Aj5015Settings Default { get; } = new Aj5015SettingsRaw
    {
        MissingIndexSuppressions =
        [
            new MissingIndexSuppressionSettingsRaw { FullColumnNamePattern = "*.sys.*", SuppressionReason = "Built-in schema" }
        ]
    }.ToSettings();

    public static string DiagnosticId => "Aj5015";
}
