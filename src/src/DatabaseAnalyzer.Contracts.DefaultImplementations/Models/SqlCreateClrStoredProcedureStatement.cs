using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

public sealed record SqlCreateClrStoredProcedureStatement(
    string SchemaName,
    string Name,
    bool IsCreateOrAlter,
    IReadOnlyList<ParameterInformation> Parameters,
    CodeRegion CodeRegion,
    SqlNullStatement CreationStatement
);
