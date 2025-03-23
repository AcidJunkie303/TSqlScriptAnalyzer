using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5022")]
internal sealed class Aj5022SettingsRaw : IRawDiagnosticSettings<Aj5022Settings>
{
    [Description("Indicates whether IF statements require BEGIN/END blocks. Default is true.")]
    public bool IfRequiresBeginEndBlock { get; set; }

    [Description("Indicates whether WHILE statements require BEGIN/END blocks. Default is true.")]
    public bool WhileRequiresBeginEndBlock { get; set; }

    public Aj5022Settings ToSettings() => new
    (
        IfRequiresBeginEndBlock,
        WhileRequiresBeginEndBlock
    );
}

public sealed record Aj5022Settings(
    bool IfRequiresBeginEndBlock,
    bool WhileRequiresBeginEndBlock
) : IDiagnosticSettings<Aj5022Settings>
{
    public static Aj5022Settings Default { get; } = new(IfRequiresBeginEndBlock: true, WhileRequiresBeginEndBlock: true);
    public static string DiagnosticId => "AJ5022";
}
