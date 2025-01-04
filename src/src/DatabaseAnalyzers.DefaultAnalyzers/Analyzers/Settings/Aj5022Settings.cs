using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

// ReSharper disable once UnusedMember.Global -> is used for setting deserialization
internal sealed class Aj5022SettingsRaw : IRawSettings<Aj5022Settings>
{
    public bool IfRequiresBeginEndBlock { get; set; }
    public bool WhileRequiresBeginEndBlock { get; set; }

    public Aj5022Settings ToSettings() => new
    (
        IfRequiresBeginEndBlock,
        WhileRequiresBeginEndBlock
    );
}

internal sealed record Aj5022Settings(
    bool IfRequiresBeginEndBlock,
    bool WhileRequiresBeginEndBlock
) : ISettings<Aj5022Settings>
{
    public static Aj5022Settings Default { get; } = new(IfRequiresBeginEndBlock: true, WhileRequiresBeginEndBlock: true);
    public static string DiagnosticId => "AJ5022";
}
