namespace DatabaseAnalyzer.Common.Models;

public interface ISchemaBoundObject : IDatabaseObject
{
    string SchemaName { get; }
}
