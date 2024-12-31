using Ardalis.GuardClauses;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class Aj5001SettingsRaw : IRawSettings<Aj5001Settings>
{
    public int MaxAllowedConcatenations { get; set; } = Aj5001Settings.Default.MaxAllowedConcatenations;

    public Aj5001Settings ToSettings() => new
    (
        Guard.Against.NegativeOrZero(MaxAllowedConcatenations)
    );
}

public sealed record Aj5001Settings(
    int MaxAllowedConcatenations
) : ISettings<Aj5001Settings>
{
    public static Aj5001Settings Default { get; } = new(2);
    public static string DiagnosticId => "AJ5001";
}
