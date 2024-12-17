namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record IndexInformation(
    string DatabaseName,
    string TableSchemaName,
    string TableName,
    string? IndexName,
    TableColumnIndexType IndexType,
    IReadOnlyList<string> ColumnNames,
    IReadOnlyList<string> IncludedColumnNames
);
