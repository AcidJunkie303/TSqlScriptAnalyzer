using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Core.Extensions;

namespace DatabaseAnalyzer.Core.Configuration;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global", Justification = "Setters are used during deserialization")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Instantiated during deserialization")]
internal sealed class DiagnosticsSettingsRaw
{
    public IReadOnlyCollection<string?>? DisabledDiagnostics { get; set; }

    public DiagnosticsSettings ToSettings() => new
    (
        DisabledDiagnostics
            .EmptyIfNull()
            .WhereNotNullOrWhiteSpace()
            .TrimAllStrings()
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase)
    );
}

public sealed record DiagnosticsSettings(
    FrozenSet<string> DisabledDiagnostics
);
