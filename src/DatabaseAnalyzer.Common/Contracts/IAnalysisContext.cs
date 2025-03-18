using System.Collections.Frozen;
using DatabaseAnalyzer.Contracts;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Common.Contracts;

public interface IAnalysisContext
{
    string DefaultSchemaName { get; }
    FrozenSet<string> DisabledDiagnosticIds { get; }
    IReadOnlyList<IScriptModel> ErrorFreeScripts { get; }
    IIssueReporter IssueReporter { get; }
    ILogger Logger { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
}
