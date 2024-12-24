namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

#pragma warning disable
public enum TableSourceType
{
    Unknown = 0,
    TableOrView = 1,
    Cte = 2,
    TempTable = 3
}
