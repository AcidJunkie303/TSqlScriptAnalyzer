namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record SchemaInformation(
    string DatabaseName,
    string SchemaName
) : ISchemaBoundObject;
