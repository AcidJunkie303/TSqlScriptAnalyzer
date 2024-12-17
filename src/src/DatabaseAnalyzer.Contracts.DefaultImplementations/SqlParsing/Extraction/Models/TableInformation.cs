namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

internal sealed class TableInformationRaw
{
    public string? DatabaseName { get; set; }
    public string? SchemaName { get; set; }
    public string? TableName { get; set; }
    public IReadOnlyList<ColumnInformation>? Columns { get; set; }
    public IReadOnlyList<IndexInformation>? Indices { get; set; }
    public IReadOnlyList<ForeignKeyConstraintInformation>? ForeignKeys { get; set; }
}

public sealed record TableInformation(
    string DatabaseName,
    string SchemaName,
    string TableName,
    IReadOnlyList<ColumnInformation> Columns,
    IReadOnlyList<IndexInformation> Indices,
    IReadOnlyList<ForeignKeyConstraintInformation> ForeignKeys
);
