namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DatabaseName { get; }
    string DefaultSchemaName { get; }
    IReadOnlyList<ScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, ScriptModel> ScriptsByDatabaseName { get; }
    IDiagnosticSettingsRetriever DiagnosticSettingsRetriever { get; }
    IIssueReporter IssueReporter { get; }
}
