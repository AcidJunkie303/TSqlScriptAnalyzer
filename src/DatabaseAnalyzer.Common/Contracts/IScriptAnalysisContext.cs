namespace DatabaseAnalyzer.Common.Contracts;

public interface IScriptAnalysisContext : IAnalysisContext
{
    IScriptModel Script { get; }
}
