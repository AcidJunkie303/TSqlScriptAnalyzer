using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts;

public interface IScriptModel
{
    string DatabaseName { get; }
    string RelativeScriptFilePath { get; }
    string Contents { get; }
    SqlScript ParsedScript { get; }
    IReadOnlyList<string> Errors { get; }
    IReadOnlyList<DiagnosticSuppression> DiagnosticSuppressions { get; }
}
