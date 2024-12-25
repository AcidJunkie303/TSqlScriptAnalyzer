namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record TableInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    IReadOnlyList<ColumnInformation> Columns,
    IReadOnlyList<IndexInformation> Indices,
    IReadOnlyList<ForeignKeyConstraintInformation> ForeignKeys,
    string RelativeScriptFilePath
) : ISchemaBoundObject;
