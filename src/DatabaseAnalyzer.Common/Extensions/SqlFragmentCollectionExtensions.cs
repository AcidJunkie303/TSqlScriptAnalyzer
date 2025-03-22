using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.Extensions;

public static class SqlFragmentCollectionExtensions
{
    public static CodeRegion CreateCodeRegionSpan<T>(this IList<T> fragments)
        where T : TSqlFragment
    {
        if (fragments.Count == 0)
        {
            throw new ArgumentException("Fragments empty.", nameof(fragments));
        }

        return CodeRegion.CreateSpan(fragments[0], fragments[^1]);
    }
}
