using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;

public sealed record ViewInformation(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    IReadOnlyList<string> Columns,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public required IScriptModel ScriptModel { get; init; }
    public string FullName { get; } = $"{DatabaseName}.{SchemaName}.{ObjectName}";

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        ObjectName
    }.ToImmutableArray();
}
