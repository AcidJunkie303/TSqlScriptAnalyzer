using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class CreateTableStatementExtensions
{
    public static bool IsTempTable(this CreateTableStatement statement)
    {
        var tableName = statement.SchemaObjectName?.BaseIdentifier?.Value;
        return tableName?.IsTempTableName() ?? false;
    }
}
