using System.Collections.Immutable;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record ColumnInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string ObjectName,
    bool IsNullable,
    bool IsUnique,
    ColumnDefinition ColumnDefinition,
    TSqlFragment CreationStatement,
    string RelativeScriptFilePath
) : ISchemaBoundObject
{
    public string FullColumnName { get; } = $"{DatabaseName}.{SchemaName}.{TableName}.{ObjectName}";

    public IReadOnlyList<string> FullNameParts { get; } = new[]
    {
        DatabaseName,
        SchemaName,
        TableName,
        ObjectName
    }.ToImmutableArray();
}
