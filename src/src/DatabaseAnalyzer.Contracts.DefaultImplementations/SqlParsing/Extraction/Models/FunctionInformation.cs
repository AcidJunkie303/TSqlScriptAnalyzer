using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;

public sealed record FunctionInformation(
    string DatabaseName,
    string SchemaName,
    string FunctionName,
    IReadOnlyList<ParameterInformation> Parameters,
    FunctionStatementBody CreationStatement
) : ISchemaBoundObject;
