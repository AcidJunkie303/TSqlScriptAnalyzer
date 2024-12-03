namespace DatabaseAnalyzer.Contracts;

public interface IScriptAnalyzer : IObjectAnalyzer
{
    void AnalyzeScript(IAnalysisContext context, IScriptModel script);
}
