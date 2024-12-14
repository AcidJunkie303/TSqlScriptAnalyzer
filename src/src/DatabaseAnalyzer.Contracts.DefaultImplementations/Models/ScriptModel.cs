using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed record ScriptModel(
    string DatabaseName,
    string RelativeScriptFilePath,
    string Contents,
    SqlScript ParsedScript,
    IReadOnlyList<string> Errors,
    IReadOnlyList<DiagnosticSuppression> DiagnosticSuppressions
) : IScriptModel;
