namespace DatabaseAnalyzer.Contracts;

public interface IGlobalAnalyzer : IObjectAnalyzer
{
    void AnalyzeScript(IAnalysisContext context);
}
