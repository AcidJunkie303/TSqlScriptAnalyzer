using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5056")]
internal sealed class Aj5056SettingsRaw : IRawSettings<Aj5056Settings>
{
    public Aj5056SKeywordNamingPolicy KeywordNamingPolicy { get; set; }

    public Aj5056Settings ToSettings() => new
    (
        KeywordNamingPolicy
    );
}

public sealed record Aj5056Settings(
    [property: Description("A policy which describes which casing type keyword must use. Possible values: `Disabled`, `UpperCase`, `LowerCase`, `CamelCase` or `PascalCase`. Default is `UpperCase`.")]
    Aj5056SKeywordNamingPolicy KeywordNamingPolicy
) : ISettings<Aj5056Settings>
{
    public static Aj5056Settings Default { get; } = new(Aj5056SKeywordNamingPolicy.UpperCase);

    public static string DiagnosticId => "AJ5056";
}
