using System.Collections.Frozen;

namespace DatabaseAnalyzer.Common.Contracts;

public interface IAnalysisContext
{
    string DefaultSchemaName { get; }
    FrozenSet<string> DisabledDiagnosticIds { get; }
    IReadOnlyList<IScriptModel> ErrorFreeScripts { get; }
    IReadOnlyList<IScriptModel> Scripts { get; }
    IReadOnlyDictionary<string, IReadOnlyList<IScriptModel>> ScriptsByDatabaseName { get; }
}
