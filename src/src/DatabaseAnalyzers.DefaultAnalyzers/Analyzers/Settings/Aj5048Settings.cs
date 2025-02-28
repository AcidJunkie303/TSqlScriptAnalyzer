using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5048SettingsRaw : IRawSettings<Aj5048Settings>
{
    public KeywordNamingPolicy KeywordNamingPolicy { get; set; }

    public Aj5048Settings ToSettings() => new
    (
        KeywordNamingPolicy
    );
}

internal sealed record Aj5048Settings(
    [property: Description("A policy which describes which casing type keyword must use. Possible values: `Disabled`, `UpperCase`, `LowerCase`, `CamelCase` or `PascalCase`. Default is `UpperCase`.")]
    KeywordNamingPolicy KeywordNamingPolicy
) : ISettings<Aj5048Settings>
{
    public static Aj5048Settings Default { get; } = new(KeywordNamingPolicy.UpperCase);
    public static string DiagnosticId => "AJ5048";
}

[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Belongs to this setting only. No point of creating a new file.")]
internal enum KeywordNamingPolicy
{
    Disabled = 0,
    UpperCase = 1,
    LowerCase = 2,
    CamelCase = 3,
    PascalCase = 4
}
