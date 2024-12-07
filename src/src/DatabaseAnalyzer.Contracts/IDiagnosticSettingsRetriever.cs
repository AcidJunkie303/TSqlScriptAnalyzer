namespace DatabaseAnalyzer.Contracts;

public interface IDiagnosticSettingsRetriever
{
    T? GetSettings<T>(string diagnosticId)
        where T : class;
}
