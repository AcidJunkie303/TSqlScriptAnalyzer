using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlNullStatementExtensions
{
    public static SqlCreateClrStoredProcedureStatement? TryParseCreateClrStoredProcedureStatement(this SqlNullStatement codeObject, string defaultSchemaName)
        => ClrStoredProcedureParser.TryParse(codeObject, defaultSchemaName);
}
