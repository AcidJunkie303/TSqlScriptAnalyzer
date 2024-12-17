namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record ProcedureInformation
(
    string SchemaName,
    string ProcedureName
);
