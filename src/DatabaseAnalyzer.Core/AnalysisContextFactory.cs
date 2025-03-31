using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Core.Models;

namespace DatabaseAnalyzer.Core;

internal sealed class AnalysisContextFactory
{
    private readonly string _defaultSchema;
    private readonly FrozenSet<string> _disabledDiagnosticIds;
    private readonly FrozenDictionary<string, IReadOnlyList<IScriptModel>> _scriptByDatabaseName;
    private readonly IReadOnlyList<IScriptModel> _scripts;

    public AnalysisContextFactory
    (
        string defaultSchema,
        IReadOnlyList<IScriptModel> scripts,
        FrozenDictionary<string, IReadOnlyList<IScriptModel>> scriptByDatabaseName,
        FrozenSet<string> disabledDiagnosticIds)
    {
        _defaultSchema = defaultSchema;
        _scripts = scripts;
        _scriptByDatabaseName = scriptByDatabaseName;
        _disabledDiagnosticIds = disabledDiagnosticIds;
    }

    public IScriptAnalysisContext CreateForScriptAnalyzer(IScriptModel script)
        => new ScriptAnalysisContext
        (
            _defaultSchema,
            _scripts,
            script,
            _scriptByDatabaseName,
            _disabledDiagnosticIds
        );

    public IGlobalAnalysisContext CreateForGlobalAnalyzer()
        => new GlobalAnalysisContext
        (
            _defaultSchema,
            _scripts,
            _scriptByDatabaseName,
            _disabledDiagnosticIds
        );
}
