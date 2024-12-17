namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

[Flags]
public enum TableColumnIndexType
{
    None = 0,
    PrimaryKey = 1,
    Clustered = 2,
    Unique = 4
}
