using System.Collections.Frozen;
using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Core.Models;

internal sealed class GlobalAnalysisContext : IGlobalAnalysisContext
{
    public IReadOnlyList<IScriptModel> ErrorFreeScripts { get; }

    // TODO:
    public IGlobalAnalysisContextServices Services { get; }
    public string DefaultSchemaName { get; }
    public IReadOnlyList<IScriptModel> Scripts { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
    public IIssueReporter IssueReporter { get; }
    public ILogger Logger { get; }
    public FrozenSet<string> DisabledDiagnosticIds { get; }

    public GlobalAnalysisContext(string defaultSchemaName,
                                 IReadOnlyList<IScriptModel> scripts,
                                 IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> scriptsByDatabaseName,
                                 IIssueReporter issueReporter,
                                 ILogger logger,
                                 IAstService astService,
                                 FrozenSet<string> disabledDiagnosticIds)
    {
        ErrorFreeScripts = scripts.Where(a => !a.HasErrors).ToImmutableArray();
        DefaultSchemaName = defaultSchemaName;
        Scripts = scripts;
        ScriptsByDatabaseName = scriptsByDatabaseName;
        IssueReporter = issueReporter;
        Logger = logger;
        DisabledDiagnosticIds = disabledDiagnosticIds;
        Services = new GlobalAnalysisContextServices(this, astService);
    }
}
