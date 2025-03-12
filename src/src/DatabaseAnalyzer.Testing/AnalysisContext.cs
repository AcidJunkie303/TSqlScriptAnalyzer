using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;

namespace DatabaseAnalyzer.Testing;

internal sealed record AnalysisContext(
    string DefaultSchemaName,
    IReadOnlyList<IScriptModel> Scripts,
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName,
    IDiagnosticSettingsProvider DiagnosticSettingsProvider,
    IIssueReporter IssueReporter
)
    : IAnalysisContext
{
    public IReadOnlyList<IScriptModel> ErrorFreeScripts { get; } = Scripts.Where(a => !a.HasErrors).ToImmutableArray();
}
