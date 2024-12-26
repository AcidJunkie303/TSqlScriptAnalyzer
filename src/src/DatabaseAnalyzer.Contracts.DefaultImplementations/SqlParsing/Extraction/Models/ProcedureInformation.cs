using System.Collections.Immutable;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record ProcedureInformation(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    IReadOnlyList<ParameterInformation> Parameters,
    ProcedureStatementBody CreationStatement,
    string RelativeScriptFilePath)
    : ISchemaBoundObject
{
    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ObjectName
    }.ToImmutableArray();

    TSqlFragment IDatabaseObject.CreationStatement => CreationStatement;
}
