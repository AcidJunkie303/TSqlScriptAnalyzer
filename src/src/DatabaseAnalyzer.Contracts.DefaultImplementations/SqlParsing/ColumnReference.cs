using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public sealed record ColumnReference(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string ColumnName,
    TableSourceType SourceType,
    TSqlFragment Fragment,
    string UsedIn
)
{
    public string FullName { get; } = $"{DatabaseName}.{SchemaName}.{TableName}.{ColumnName}";
}
