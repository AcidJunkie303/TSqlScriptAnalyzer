namespace DatabaseAnalyzer.Contracts;

public interface IScriptAnalyzer : IObjectAnalyzer
{
    void AnalyzeScript(IAnalysisContext context, ScriptModel script);
}
