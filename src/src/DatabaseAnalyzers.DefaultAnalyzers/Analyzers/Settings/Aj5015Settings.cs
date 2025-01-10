using System.Collections.Immutable;
using System.ComponentModel;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5015SettingsRaw : IRawSettings<Aj5015Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyList<MissingIndexSuppressionSettingsRaw>? MissingIndexSuppressions { get; set; }

    public Aj5015Settings ToSettings()
        => MissingIndexSuppressions is null
            ? Aj5015Settings.Default
            : new Aj5015Settings(MissingIndexSuppressions.Select(static a => a.ToSettings()).ToImmutableArray());
}

internal sealed record Aj5015Settings(
    [property: Description("List of of suppressed missing index warnings. For 'FullColumnNamePattern', Wildcards like `*` and `?` are supported.")]
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

    public static string DiagnosticId => "AJ5015";
}
