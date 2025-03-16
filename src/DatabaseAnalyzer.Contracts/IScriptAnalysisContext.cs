namespace DatabaseAnalyzer.Contracts;

public interface IScriptAnalysisContext : IAnalysisContext
{
    IScriptModel Script { get; }
}
