using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed record AnalysisContext(
    string DefaultSchemaName,
    IReadOnlyList<IScriptModel> Scripts,
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName,
    IDiagnosticSettingsProvider DiagnosticSettingsProvider,
    IIssueReporter IssueReporter
) : IAnalysisContext;
