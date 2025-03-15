namespace DatabaseAnalyzer.Contracts;

public interface IGlobalAnalyzer : IObjectAnalyzer
{
    void Analyze(IAnalysisContext context);
    void Analyze();
}
