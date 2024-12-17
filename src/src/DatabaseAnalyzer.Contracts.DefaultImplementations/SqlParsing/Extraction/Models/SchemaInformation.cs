namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed class SchemaInformationRaw
{
    public string? SchemaName { get; set; }
    public IReadOnlyDictionary<string, TableInformation>? TablesByName { get; set; }
    public IReadOnlyDictionary<string, ProcedureInformation>? ProceduresByName { get; set; }
    public IReadOnlyDictionary<string, FunctionInformation>? FunctionsByName { get; set; }
}

public sealed record SchemaInformation(
    string SchemaName,
    IReadOnlyDictionary<string, TableInformation> TablesByName
);
