namespace DatabaseAnalyzer.Common.Models;

public sealed record DatabaseInformation(
    string DatabaseName,
    IReadOnlyDictionary<string, SchemaInformationWithObjects> SchemasByName
);
