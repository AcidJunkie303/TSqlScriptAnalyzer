using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5045")]
internal sealed class Aj5045SettingsRaw : IRawDiagnosticSettings<Aj5045Settings>
{
    [Description("An empty line is required before a GO statement. Default is false.")]
    public bool RequireEmptyLineBeforeGo { get; set; }

    [Description("An empty line is required after a GO statement. Default is false.")]
    public bool RequireEmptyLineAfterGo { get; set; }

    public Aj5045Settings ToSettings() => new
    (
        RequireEmptyLineBeforeGo: RequireEmptyLineBeforeGo,
        RequireEmptyLineAfterGo: RequireEmptyLineAfterGo
    );
}

public sealed record Aj5045Settings(
    bool RequireEmptyLineBeforeGo,
    bool RequireEmptyLineAfterGo
) : IDiagnosticSettings<Aj5045Settings>
{
    public static Aj5045Settings Default { get; } = new(RequireEmptyLineBeforeGo: false, RequireEmptyLineAfterGo: false);
    public static string DiagnosticId => "AJ5045";
}
