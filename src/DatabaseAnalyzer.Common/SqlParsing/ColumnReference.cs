using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

public sealed record ColumnReference(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string ColumnName,
    TableSourceType SourceType,
    TSqlFragment Fragment,
    string UsedIn,
    string? SourceAliasName
)
{
    public string FullName { get; } = $"{DatabaseName}.{SchemaName}.{TableName}.{ColumnName}";

    public static ColumnReference MissingAliasColumnReference { get; } = new
    (
        string.Empty,
        string.Empty,
        string.Empty,
        string.Empty,
        TableSourceType.Unknown,
        new ColumnReferenceExpression(),
        string.Empty,
        string.Empty
    );
}
