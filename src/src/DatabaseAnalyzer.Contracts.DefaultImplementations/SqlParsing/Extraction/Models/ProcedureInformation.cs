using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record ProcedureInformation(
    string DatabaseName,
    string SchemaName,
    string ProcedureName,
    IReadOnlyList<ParameterInformation> Parameters,
    ProcedureStatementBody CreationStatement
) : ISchemaBoundObject;
