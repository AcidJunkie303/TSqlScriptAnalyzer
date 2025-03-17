using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Testing;

internal sealed record GlobalAnalysisContext(
    string DefaultSchemaName,
    IReadOnlyList<IScriptModel> Scripts,
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName,
    IIssueReporter IssueReporter,
    ILogger Logger,
    FrozenSet<string> DisabledDiagnosticIds
)
    : IGlobalAnalysisContext
{
    public IReadOnlyList<IScriptModel> ErrorFreeScripts { get; } = Scripts.Where(a => !a.HasErrors).ToImmutableArray();

    // TODO:
    public IGlobalAnalysisContextServices Services => null!;
}
