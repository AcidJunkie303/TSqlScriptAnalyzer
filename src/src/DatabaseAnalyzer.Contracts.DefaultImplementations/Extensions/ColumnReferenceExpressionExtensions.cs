using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class ColumnReferenceExpressionExtensions
{
    public static string GetReferenceName(this ColumnReferenceExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return string.Join('.', expression.MultiPartIdentifier.Identifiers.Select(static a => a.Value));
    }
}
