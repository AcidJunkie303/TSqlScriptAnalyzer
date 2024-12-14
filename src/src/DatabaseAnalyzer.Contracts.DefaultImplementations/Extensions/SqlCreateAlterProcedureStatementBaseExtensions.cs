using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlCreateAlterProcedureStatementBaseExtensions
{
    public static SqlCompoundStatement? TryGetBody(this SqlCreateAlterProcedureStatementBase procedure) =>
        procedure.GetDescendantsOfType<SqlCompoundStatement>().SingleOrDefault();
}
