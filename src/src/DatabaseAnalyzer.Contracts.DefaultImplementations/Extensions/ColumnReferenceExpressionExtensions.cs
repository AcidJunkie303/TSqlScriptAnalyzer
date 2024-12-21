using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class ColumnReferenceExpressionExtensions
{
    public static string GetReferenceName(this ColumnReferenceExpression expression)
        => string.Join(".", expression.MultiPartIdentifier.Identifiers.Select(a => a.Value));
}
