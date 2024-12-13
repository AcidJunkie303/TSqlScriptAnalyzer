namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DefaultSchemaName { get; }
    IReadOnlyList<ScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IReadOnlyList<ScriptModel>> ScriptsByDatabaseName { get; }
    IDiagnosticSettingsRetriever DiagnosticSettingsRetriever { get; }
    IIssueReporter IssueReporter { get; }
}
