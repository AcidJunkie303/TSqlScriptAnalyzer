using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5048SettingsRaw : IRawSettings<Aj5048Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public Aj5048KeywordNotationType KeywordNotationType { get; set; }

    public Aj5048Settings ToSettings() => new
    (
        KeywordNotationType
    );
}

internal sealed record Aj5048Settings(
    [property: Description("The keyword notation type. Supported are: `None`, `Long` or `Short`.")]
    Aj5048KeywordNotationType KeywordNotationType
) : ISettings<Aj5048Settings>
{
    public static string DiagnosticId => "AJ5048";

    public static Aj5048Settings Default { get; } = new Aj5048SettingsRaw
    {
        KeywordNotationType = Aj5048KeywordNotationType.None
    }.ToSettings();
}
