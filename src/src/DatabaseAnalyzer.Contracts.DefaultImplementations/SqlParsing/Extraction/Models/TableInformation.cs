using System.Collections.Immutable;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record TableInformation(
    string DatabaseName,
    string SchemaName,
    string ObjectName,
    IReadOnlyList<ColumnInformation> Columns,
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
