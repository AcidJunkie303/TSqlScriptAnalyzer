namespace DatabaseAnalyzer.Contracts;

public interface IScriptAnalyzer : IObjectAnalyzer
{
    void AnalyzeScript(IAnalysisContext context, IScript script);
}
