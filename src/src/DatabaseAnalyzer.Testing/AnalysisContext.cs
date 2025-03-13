using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Testing;

internal sealed record AnalysisContext(
    string DefaultSchemaName,
    IReadOnlyList<IScriptModel> Scripts,
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName,
    IDiagnosticSettingsProvider DiagnosticSettingsProvider,
    IIssueReporter IssueReporter,
    ILogger Logger
)
    : IAnalysisContext
{
    public IReadOnlyList<IScriptModel> ErrorFreeScripts { get; } = Scripts.Where(a => !a.HasErrors).ToImmutableArray();
}
