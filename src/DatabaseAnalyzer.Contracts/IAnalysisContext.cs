using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Contracts;

public interface IAnalysisContext
{
    string DefaultSchemaName { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyList<IScriptModel> ErrorFreeScripts { get; }
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
    IDiagnosticSettingsProvider DiagnosticSettingsProvider { get; }
    IIssueReporter IssueReporter { get; }
    ILogger Logger { get; }
    FrozenSet<string> DisabledDiagnosticIds { get; }
}
