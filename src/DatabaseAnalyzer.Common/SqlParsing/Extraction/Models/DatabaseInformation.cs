namespace DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;

public sealed record DatabaseInformation(
    string DatabaseName,
    IReadOnlyDictionary<string, SchemaInformationWithObjects> SchemasByName
);
