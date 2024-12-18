namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record DatabaseInformation(
    string DatabaseName,
    IReadOnlyDictionary<string, SchemaInformationWithObjects> SchemasByName
);
