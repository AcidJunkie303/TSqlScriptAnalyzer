using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Core.Services;
using Microsoft.Extensions.Logging;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
internal sealed class ScriptAnalysisContext : IScriptAnalysisContext
{
    public IReadOnlyList<IScriptModel> ErrorFreeScripts { get; }
    public IScriptAnalysisContextServices Services { get; }
    public string DefaultSchemaName { get; init; }
    public IReadOnlyList<IScriptModel> Scripts { get; init; }
    public IScriptModel Script { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; init; }
    public IIssueReporter IssueReporter { get; init; }
    public ILogger Logger { get; init; }
    public IAstService AstService { get; }
    public FrozenSet<string> DisabledDiagnosticIds { get; init; }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
    public ScriptAnalysisContext(string defaultSchemaName,
                                 IReadOnlyList<IScriptModel> scripts,
                                 IScriptModel script,
                                 IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> scriptsByDatabaseName,
                                 IIssueReporter issueReporter,
                                 ILogger logger,
                                 IAstService astService,
                                 FrozenSet<string> disabledDiagnosticIds)
    {
        ErrorFreeScripts = scripts.Where(a => !a.HasErrors).ToImmutableArray();
        DefaultSchemaName = defaultSchemaName;
        Scripts = scripts;
        Script = script;
        ScriptsByDatabaseName = scriptsByDatabaseName;
        IssueReporter = issueReporter;
        Logger = logger;
        AstService = astService;
        DisabledDiagnosticIds = disabledDiagnosticIds;
        Services = new ScriptAnalysisContextServices(this, astService);
    }
}
