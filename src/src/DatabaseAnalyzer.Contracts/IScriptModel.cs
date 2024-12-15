using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts;

public interface IScriptModel
{
    string DatabaseName { get; }
    string RelativeScriptFilePath { get; }
    string Contents { get; }
    TSqlScript ParsedScript { get; }
    IReadOnlyList<string> Errors { get; }
    IReadOnlyList<DiagnosticSuppression> DiagnosticSuppressions { get; }
}
