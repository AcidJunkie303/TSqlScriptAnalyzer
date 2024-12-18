namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record SchemaInformationWithObjects(
    string DatabaseName,
    string SchemaName,
    IReadOnlyDictionary<string, TableInformation> TablesByName,
    IReadOnlyDictionary<string, ProcedureInformation> ProceduresByName,
    IReadOnlyDictionary<string, FunctionInformation> FunctionsByName
) : ISchemaBoundObject;
