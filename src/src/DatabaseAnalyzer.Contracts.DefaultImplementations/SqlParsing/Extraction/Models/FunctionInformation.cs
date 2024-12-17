namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record FunctionInformation(
    string SchemaName,
    string FunctionName
);
