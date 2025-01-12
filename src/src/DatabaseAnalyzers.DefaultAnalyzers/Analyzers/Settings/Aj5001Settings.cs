using System.ComponentModel;
using Ardalis.GuardClauses;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5001SettingsRaw : IRawSettings<Aj5001Settings>
{
    public int MaxAllowedConcatenations { get; set; } = Aj5001Settings.Default.MaxAllowedConcatenations;

    public Aj5001Settings ToSettings() => new
    (
        Guard.Against.NegativeOrZero(MaxAllowedConcatenations)
    );
}

internal sealed record Aj5001Settings(
    [property: Description("Defines how many concatenations are tolerated. A value of 0 means no limit.")]
    int MaxAllowedConcatenations
) : ISettings<Aj5001Settings>
{
    public static Aj5001Settings Default { get; } = new(2);
    public static string DiagnosticId => "AJ5001";
}
