using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Common.Contracts;

namespace DatabaseAnalyzer.Testing;

[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters")]
internal sealed class ScriptAnalysisContext : IScriptAnalysisContext
{
    public IReadOnlyList<IScriptModel> ErrorFreeScripts { get; }
    public string DefaultSchemaName { get; }
    public IReadOnlyList<IScriptModel> Scripts { get; }
    public IScriptModel Script { get; }
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
