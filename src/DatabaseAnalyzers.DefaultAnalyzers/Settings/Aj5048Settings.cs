using System.ComponentModel;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
[SettingsSource(SettingsSourceKind.Diagnostics, "AJ5048")]
internal sealed class Aj5048SettingsRaw : IRawDiagnosticSettings<Aj5048Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    [Description("The keyword notation type for `Execute | Exec`. Supported are: `None`, `Long` or `Short`. Default is `Short`.")]
    public Aj5048KeywordNotationType? Execute { get; set; }

    [Description("The keyword notation type for `Procedure | Proc`. Supported are: `None`, `Long` or `Short`. Default is `Long`.")]
    public Aj5048KeywordNotationType? Procedure { get; set; }

    [Description("The keyword notation type for `Transaction | Tran`. Supported are: `None`, `Long` or `Short`. Default is `Long`.")]
    public Aj5048KeywordNotationType? Transaction { get; set; }

    public Aj5048Settings ToSettings() => new
    (
        Execute ?? Aj5048KeywordNotationType.Short,
        Procedure ?? Aj5048KeywordNotationType.Long,
        Transaction ?? Aj5048KeywordNotationType.Long
    );
}

public sealed record Aj5048Settings(
    Aj5048KeywordNotationType Execute,
    Aj5048KeywordNotationType Procedure,
    Aj5048KeywordNotationType Transaction
) : IDiagnosticSettings<Aj5048Settings>
{
    public bool IsEnabled { get; } = Execute != Aj5048KeywordNotationType.None
                                     || Procedure != Aj5048KeywordNotationType.None
                                     || Transaction != Aj5048KeywordNotationType.None;

    public static string DiagnosticId => "AJ5048";

    public static Aj5048Settings Default { get; } = new
    (
        Execute: Aj5048KeywordNotationType.Short,
        Procedure: Aj5048KeywordNotationType.Long,
        Transaction: Aj5048KeywordNotationType.Long
    );
}
