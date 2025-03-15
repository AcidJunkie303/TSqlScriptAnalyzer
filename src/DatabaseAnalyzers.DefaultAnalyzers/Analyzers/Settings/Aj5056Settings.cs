using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5056")]
internal sealed class Aj5056SettingsRaw : IRawSettings<Aj5056Settings>
{
    public KeywordNamingPolicy KeywordNamingPolicy { get; set; }

    public Aj5056Settings ToSettings() => new
    (
        KeywordNamingPolicy
    );
}

internal sealed record Aj5056Settings(
    [property: Description("A policy which describes which casing type keyword must use. Possible values: `Disabled`, `UpperCase`, `LowerCase`, `CamelCase` or `PascalCase`. Default is `UpperCase`.")]
    KeywordNamingPolicy KeywordNamingPolicy
) : ISettings<Aj5056Settings>
{
    public static Aj5056Settings Default { get; } = new(KeywordNamingPolicy.UpperCase);

    public static string DiagnosticId => "AJ5056";
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
