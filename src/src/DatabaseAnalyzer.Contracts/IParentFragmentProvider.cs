using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts;

public interface IParentFragmentProvider
{
    TSqlFragment Root { get; }
    TSqlFragment? GetParent(TSqlFragment fragment);

    IEnumerable<TSqlFragment> GetParents(TSqlFragment fragment)
    {
        var parent = GetParent(fragment);

        while (parent is not null)
        {
            yield return parent;

            parent = GetParent(parent);
        }
    }
}
