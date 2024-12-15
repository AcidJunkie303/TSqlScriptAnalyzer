using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed record ScriptModel(
    string DatabaseName,
    string RelativeScriptFilePath,
    string Contents,
    TSqlScript ParsedScript,
    IParentFragmentProvider ParentFragmentProvider,
    IReadOnlyList<string> Errors,
    IReadOnlyList<DiagnosticSuppression> DiagnosticSuppressions
) : IScriptModel;
