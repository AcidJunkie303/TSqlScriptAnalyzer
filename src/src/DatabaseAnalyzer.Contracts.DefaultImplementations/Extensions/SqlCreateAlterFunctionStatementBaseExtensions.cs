using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlCreateAlterFunctionStatementBaseExtensions
{
    public static SqlFunctionBodyDefinition? TryGetBody(this SqlCreateAlterFunctionStatementBase function) => function
        .GetDescendantsOfType<SqlFunctionBodyDefinition>()
        .FirstOrDefault();
}
