using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Core.Models;

internal sealed class ScriptAnalysisContext : IScriptAnalysisContext
{
    public IScriptModel Script { get; }
    public IReadOnlyList<IScriptModel> ErrorFreeScripts { get; set; }
    public string DefaultSchemaName { get; }
    public IReadOnlyList<IScriptModel> Scripts { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
    public FrozenSet<string> DisabledDiagnosticIds { get; }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
    public ScriptAnalysisContext(string defaultSchemaName,
                                 IReadOnlyList<IScriptModel> scripts,
                                 IScriptModel script,
                                 IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> scriptsByDatabaseName,
                                 FrozenSet<string> disabledDiagnosticIds)
    {
        ErrorFreeScripts = scripts.Where(static a => !a.HasErrors).ToImmutableArray();
        DefaultSchemaName = defaultSchemaName;
        Scripts = scripts;
        Script = script;
        ScriptsByDatabaseName = scriptsByDatabaseName;
        DisabledDiagnosticIds = disabledDiagnosticIds;
    }
}
