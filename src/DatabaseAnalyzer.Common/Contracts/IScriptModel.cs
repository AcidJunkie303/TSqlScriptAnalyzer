using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Contracts;

public interface IScriptModel
{
    string DatabaseName { get; }
    string RelativeScriptFilePath { get; }
    string Contents { get; }
    TSqlScript ParsedScript { get; }
    IParentFragmentProvider ParentFragmentProvider { get; }
    IReadOnlyList<ScriptError> Errors { get; }
    IReadOnlyList<DiagnosticSuppression> DiagnosticSuppressions { get; }
    bool HasErrors => Errors.Count > 0;
}
