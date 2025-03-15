using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5057")]
internal sealed class Aj5057SettingsRaw : IRawSettings<Aj5057Settings>
{
    public IReadOnlyDictionary<string, string?>? CasingByIdentifier { get; set; }

    public Aj5057Settings ToSettings() => new
    (
        CasingByIdentifier
            ?.Where(a => !a.Value.IsNullOrWhiteSpace())
            .ToFrozenDictionary(a => a.Key, a => a.Value!, StringComparer.OrdinalIgnoreCase)
        ?? FrozenDictionary<string, string>.Empty
    );
}

internal sealed record Aj5057Settings(
    [property: Description("Casing by identifier name.")]
    IReadOnlyDictionary<string, string> CasingByIdentifier
) : ISettings<Aj5057Settings>
{
    public static Aj5057Settings Default { get; } = new(FrozenDictionary<string, string>.Empty);

    public static string DiagnosticId => "AJ5057";
}
