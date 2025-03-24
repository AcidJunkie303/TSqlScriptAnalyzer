using System.Collections.Frozen;
using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;
using DatabaseAnalyzer.Common.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5057")]
internal sealed class Aj5057SettingsRaw : IRawDiagnosticSettings<Aj5057Settings>
{
    [Description("Casing by identifier name.")]
    public IReadOnlyDictionary<string, string?>? CasingByIdentifier { get; set; }

    public Aj5057Settings ToSettings() => new
    (
        CasingByIdentifier
            ?.Where(static a => !a.Value.IsNullOrWhiteSpace())
            .ToFrozenDictionary(
                static a => a.Key,
                static a => a.Value!,
                StringComparer.OrdinalIgnoreCase)
        ?? FrozenDictionary<string, string>.Empty
    );
}

public sealed record Aj5057Settings(
    IReadOnlyDictionary<string, string> CasingByIdentifier
) : IDiagnosticSettings<Aj5057Settings>
{
    public static Aj5057Settings Default { get; } = new(FrozenDictionary<string, string>.Empty);

    public static string DiagnosticId => "AJ5057";
}
