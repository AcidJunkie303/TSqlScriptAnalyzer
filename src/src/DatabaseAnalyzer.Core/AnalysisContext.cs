using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Core;

internal sealed record AnalysisContext(
    string DefaultSchemaName,
    IReadOnlyList<ScriptModel> Scripts,
    IReadOnlyDictionary<string, IReadOnlyList<ScriptModel>> ScriptsByDatabaseName,
    IDiagnosticSettingsRetriever DiagnosticSettingsRetriever,
    IIssueReporter IssueReporter
) : IAnalysisContext;
