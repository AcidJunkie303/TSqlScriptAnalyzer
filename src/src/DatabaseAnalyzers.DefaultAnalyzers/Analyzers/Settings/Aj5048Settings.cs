using System.ComponentModel;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5048SettingsRaw : IRawSettings<Aj5048Settings>
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global -> used during deserialization
    public Aj5048KeywordNotationType? Execute { get; set; }
    public Aj5048KeywordNotationType? Procedure { get; set; }
    public Aj5048KeywordNotationType? Transaction { get; set; }

    public Aj5048Settings ToSettings() => new
    (
        Execute ?? Aj5048KeywordNotationType.Short,
        Procedure ?? Aj5048KeywordNotationType.Long,
        Transaction ?? Aj5048KeywordNotationType.Long
    );
}

internal sealed record Aj5048Settings(
    [property: Description("The keyword notation type for `Execute | Exec`. Supported are: `None`, `Long` or `Short`. Default is `Short`.")]
    Aj5048KeywordNotationType Execute,
    [property: Description("The keyword notation type for `Procedure | Proc`. Supported are: `None`, `Long` or `Short`. Default is `Long`.")]
    Aj5048KeywordNotationType Procedure,
    [property: Description("The keyword notation type for `Transaction | Tran`. Supported are: `None`, `Long` or `Short`. Default is `Long`.")]
    Aj5048KeywordNotationType Transaction
) : ISettings<Aj5048Settings>
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
