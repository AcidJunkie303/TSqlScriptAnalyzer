using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed record AnalysisContext(
    string DatabaseName,
    string DefaultSchemaName,
    IReadOnlyList<ScriptModel> Scripts,
    IReadOnlyDictionary<string, ScriptModel> ScriptsByDatabaseName,
    IDiagnosticSettingsRetriever DiagnosticSettingsRetriever,
    IIssueReporter IssueReporter
) : IAnalysisContext
{
    public IReadOnlyList<ScriptModel> CurrentDatabaseScripts { get; } = Scripts
        .Where(a => a.DatabaseName.EqualsOrdinalIgnoreCase(DatabaseName))
        .ToList();
}
