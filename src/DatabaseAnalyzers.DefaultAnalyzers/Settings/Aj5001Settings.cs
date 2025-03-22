using System.ComponentModel;
using Ardalis.GuardClauses;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5001")]
internal sealed class Aj5001SettingsRaw : IRawDiagnosticSettings<Aj5001Settings>
{
    public int MaxAllowedConcatenations { get; set; } = Aj5001Settings.Default.MaxAllowedConcatenations;

    public Aj5001Settings ToSettings() => new
    (
        Guard.Against.NegativeOrZero(MaxAllowedConcatenations)
    );
}

public sealed record Aj5001Settings(
    [property: Description("Defines how many concatenations are tolerated. A value of 0 means no limit.")]
    int MaxAllowedConcatenations
) : IDiagnosticSettings<Aj5001Settings>
{
    public static Aj5001Settings Default { get; } = new(2);
    public static string DiagnosticId => "AJ5001";
}
