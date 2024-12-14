using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed record AnalysisContext(
    string DefaultSchemaName,
    IReadOnlyList<IScriptModel> Scripts,
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName,
    IDiagnosticSettingsRetriever DiagnosticSettingsRetriever,
    IIssueReporter IssueReporter
) : IAnalysisContext;
