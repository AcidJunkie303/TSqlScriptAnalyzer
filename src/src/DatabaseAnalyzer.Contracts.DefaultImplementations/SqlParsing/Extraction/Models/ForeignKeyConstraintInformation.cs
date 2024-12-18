namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record ForeignKeyConstraintInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    string ColumnName,
    string ConstraintName,
    string ReferencedTableSchemaName,
    string ReferencedTableName,
    string ReferencedTableColumnName
) : ISchemaBoundObject;
