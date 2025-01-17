namespace DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;

public interface ISchemaBoundObject : IDatabaseObject
{
    string SchemaName { get; }
}
