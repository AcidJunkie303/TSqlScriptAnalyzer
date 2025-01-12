namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public interface ISchemaBoundObject : IDatabaseObject
{
    string SchemaName { get; }
}
