using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;

public sealed record ForeignKeyConstraintInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string ColumnName,
    string ObjectName,
    string ReferencedTableSchemaName,
    string ReferencedTableName,
    string ReferencedTableColumnName,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public string FullColumnName { get; } = $"{DatabaseName}.{SchemaName}.{TableName}.{ColumnName}";

    public string FullName { get; } = new[]
    {
        DatabaseName,
        ObjectName
    }.StringJoin('.');

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        ObjectName
    }.ToImmutableArray();
}
