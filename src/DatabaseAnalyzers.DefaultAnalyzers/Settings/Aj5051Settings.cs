using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5051")]
internal sealed class Aj5051SettingsRaw : IRawDiagnosticSettings<Aj5051Settings>
{
    [Description("Indicates whether to ignore unused primary keys. Default is `false`")]
    public bool IgnoreUnusedPrimaryKeyIndices { get; set; }

    public Aj5051Settings ToSettings() => new
    (
        IgnoreUnusedPrimaryKeyIndices
    );
}

public sealed record Aj5051Settings(
    bool IgnoreUnusedPrimaryKeyIndices
) : IDiagnosticSettings<Aj5051Settings>
{
    public static Aj5051Settings Default { get; } = new(IgnoreUnusedPrimaryKeyIndices: false);
    public static string DiagnosticId => "AJ5051";
}
