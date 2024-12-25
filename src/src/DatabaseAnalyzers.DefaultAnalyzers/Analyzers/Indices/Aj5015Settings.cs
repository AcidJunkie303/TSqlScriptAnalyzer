using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class Aj5015SettingsRaw : IRawSettings<Aj5015Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public IReadOnlyDictionary<string, string?>? SuppressionReasonByFullColumnName { get; set; }

    public Aj5015Settings ToSettings() => new
    (
        (SuppressionReasonByFullColumnName ?? FrozenDictionary<string, string?>.Empty)
        .Where(a => !a.Value.IsNullOrWhiteSpace())
        .ToFrozenDictionary(a => a.Key, a => a.Value!.Trim(), StringComparer.OrdinalIgnoreCase)
    );
}

public sealed record Aj5015Settings(
    FrozenDictionary<string, string> SuppressionReasonByFullColumnName
) : ISettings<Aj5015Settings>
{
    public static Aj5015Settings Default { get; } = new(FrozenDictionary<string, string>.Empty);
    public static string DiagnosticId => "Aj5015";
}
