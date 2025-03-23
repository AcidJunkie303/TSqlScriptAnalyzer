using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record ProcedureInformation(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    IReadOnlyList<ParameterInformation> Parameters,
    IReadOnlyDictionary<string, ParameterInformation> ParametersByName,
    IReadOnlyDictionary<string, ParameterInformation> ParametersByTrimmedName,
    ProcedureStatementBody CreationStatement,
    string RelativeScriptFilePath)
    : ISchemaBoundObject
{
    public string FullName { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ObjectName
    }.StringJoin('.');

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ObjectName
    }.ToImmutableArray();

    TSqlFragment IDatabaseObject.CreationStatement => CreationStatement;
}
