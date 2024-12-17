namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record ForeignKeyConstraintInformation(
    string DatabaseName,
    string TableSchemaName,
    string TableName,
    string ConstraintName,
    string ColumnName,
    string ReferencedTableSchemaName,
    string ReferencedTableName,
    string ReferencedTableColumnName
);
