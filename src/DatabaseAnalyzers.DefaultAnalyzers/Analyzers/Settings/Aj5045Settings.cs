using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5045SettingsRaw : IRawSettings<Aj5045Settings>
{
    public bool RequireEmptyLineBeforeGo { get; set; }
    public bool RequireEmptyLineAfterGo { get; set; }

    public Aj5045Settings ToSettings() => new
    (
        RequireEmptyLineBeforeGo,
        RequireEmptyLineAfterGo
    );
}

internal sealed record Aj5045Settings(
    [property: Description("An empty line is required before a GO statement. Default is false.")]
    bool RequireEmptyLineBeforeGo,
    [property: Description("An empty line is required after a GO statement. Default is false.")]
    bool RequireEmptyLineAfterGo
) : ISettings<Aj5045Settings>
{
    public static Aj5045Settings Default { get; } = new(false, false);
    public static string DiagnosticId => "AJ5045";
}
