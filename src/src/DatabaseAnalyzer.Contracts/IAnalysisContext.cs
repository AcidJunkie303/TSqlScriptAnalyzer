namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DefaultSchemaName { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
    IDiagnosticSettingsProvider DiagnosticSettingsProvider { get; }
    IIssueReporter IssueReporter { get; }
}
