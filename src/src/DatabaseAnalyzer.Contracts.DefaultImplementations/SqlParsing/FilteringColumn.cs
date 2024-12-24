using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

internal sealed record FilteringColumn(
    string DatabaseName,
    string SchemaName,
    string? TableName,
    string ColumnName,
    TableSourceType SourceType,
    TSqlFragment Fragment
);
