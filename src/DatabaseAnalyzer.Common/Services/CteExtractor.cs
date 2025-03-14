using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Services;

public static class CteExtractor
{
    public static FrozenSet<string> ExtractCteNames(TSqlBatch batch)
        => batch
            .GetChildren(recursive: true)
            .OfType<SelectStatement>()
            .Where(a => !(a.WithCtesAndXmlNamespaces?.CommonTableExpressions).IsNullOrEmpty())
            .SelectMany(a => a.WithCtesAndXmlNamespaces.CommonTableExpressions)
            .Select(a => a.ExpressionName.Value)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
}
