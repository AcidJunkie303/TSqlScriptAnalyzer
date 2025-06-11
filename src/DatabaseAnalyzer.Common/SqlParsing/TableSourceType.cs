namespace DatabaseAnalyzer.Common.SqlParsing;

public enum TableSourceType
{
    Unknown = 0,
    NotDetermined = 1,
    TableOrView = 2,
    Cte = 3,
    TempTable = 4,
    DerivedTable = 5
}
