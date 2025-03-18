using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record ScriptModel(
    string DatabaseName,
    string RelativeScriptFilePath,
    string Contents,
    TSqlScript ParsedScript,
    IParentFragmentProvider ParentFragmentProvider,
    IReadOnlyList<ScriptError> Errors,
    IReadOnlyList<DiagnosticSuppression> DiagnosticSuppressions
) : IScriptModel;
