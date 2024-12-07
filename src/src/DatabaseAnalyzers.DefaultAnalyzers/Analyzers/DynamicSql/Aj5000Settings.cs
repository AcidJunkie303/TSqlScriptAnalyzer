using Ardalis.GuardClauses;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.DynamicSql;

public sealed class Aj5000SettingsRaw
{
    public string? ExampleValue { get; set; }

    public Aj5000Settings ToSettings() => new
    (
        Guard.Against.NullOrWhiteSpace(ExampleValue)
    );
}

public sealed record Aj5000Settings(string ExampleValue)
{
    public static Aj5000Settings Default { get; } = new(ExampleValue: "Default");
}
