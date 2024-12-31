using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class Aj5022SettingsRaw : IRawSettings<Aj5022Settings>
{
    public bool ForRequiresBeginEndBlock { get; set; }
    public bool IfRequiresBeginEndBlock { get; set; }
    public bool WhileRequiresBeginEndBlock { get; set; }

    public Aj5022Settings ToSettings() => new
    (
        ForRequiresBeginEndBlock,
        IfRequiresBeginEndBlock,
        WhileRequiresBeginEndBlock
    );
}

public sealed record Aj5022Settings(
    bool ForRequiresBeginEndBlock,
    bool IfRequiresBeginEndBlock,
    bool WhileRequiresBeginEndBlock
) : ISettings<Aj5022Settings>
{
    public static Aj5022Settings Default { get; } = new(true, true, true);
    public static string DiagnosticId => "AJ5022";
}
