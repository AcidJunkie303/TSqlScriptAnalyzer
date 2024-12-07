using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Core.Extensions;

namespace DatabaseAnalyzer.Core;

internal sealed record AnalysisContext(
    string DatabaseName,
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
