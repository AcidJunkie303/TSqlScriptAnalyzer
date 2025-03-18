namespace DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;

[Flags]
public enum TableColumnIndexTypes
{
    None = 0,
    PrimaryKey = 1,
    Clustered = 2,
    Unique = 4
}
