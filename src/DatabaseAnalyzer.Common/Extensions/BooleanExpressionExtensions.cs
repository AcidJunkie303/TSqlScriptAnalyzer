using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class BooleanExpressionExtensions
{
    public static bool IsAlwaysTruePredicate(this BooleanExpression expression)
    {
        var predicate = expression.GetSql()
            .Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);

        return predicate.EqualsOrdinal("1=1") || predicate.EqualsOrdinal("0=0");
    }
}
