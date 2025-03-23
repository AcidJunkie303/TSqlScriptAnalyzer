using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Models;

public sealed record TableInformation(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    IReadOnlyList<ColumnInformation> Columns,
    IReadOnlyDictionary<string, ColumnInformation> ColumnsByName,
    IReadOnlyList<IndexInformation> Indices,
    IReadOnlyList<ForeignKeyConstraintInformation> ForeignKeys,
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
