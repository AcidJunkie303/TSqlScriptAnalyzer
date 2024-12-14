namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DefaultSchemaName { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
    IDiagnosticSettingsRetriever DiagnosticSettingsRetriever { get; }
    IIssueReporter IssueReporter { get; }
}
