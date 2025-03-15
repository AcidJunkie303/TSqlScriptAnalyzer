using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5051")]
internal sealed class Aj5051SettingsRaw : IRawSettings<Aj5051Settings>
{
    public bool IgnoreUnusedPrimaryKeyIndices { get; set; }

    public Aj5051Settings ToSettings() => new
    (
        IgnoreUnusedPrimaryKeyIndices
    );
}

internal sealed record Aj5051Settings(
    [property: Description("Indicates whether to ignore unused primary keys. Default is `false`")]
    bool IgnoreUnusedPrimaryKeyIndices
) : ISettings<Aj5051Settings>
{
    public static Aj5051Settings Default { get; } = new(false);
    public static string DiagnosticId => "AJ5051";
}
