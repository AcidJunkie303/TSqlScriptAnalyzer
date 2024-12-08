using Ardalis.GuardClauses;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Strings;

public sealed class Aj5001SettingsRaw
{
    public int MaxAllowedConcatenations { get; set; } = Aj5001Settings.Default.MaxAllowedConcatenations;

    public Aj5001Settings ToSettings() => new
    (
        Guard.Against.NegativeOrZero(MaxAllowedConcatenations)
    );
}

public sealed record Aj5001Settings(int MaxAllowedConcatenations)
{
    public static Aj5001Settings Default { get; } = new(2);
}
