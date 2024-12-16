using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlScriptExtensions
{
    public static IParentFragmentProvider CreateParentFragmentProvider(this TSqlScript script)
        => ParentFragmentProviderFactory.Build(script);

    public static IEnumerable<T> GetTopLevelDescendantsOfType<T>(this TSqlScript script)
        where T : TSqlFragment
    {
        var visitor = new GetTopLevelDescendantVisitor<T>();
        script.Accept(visitor);
        return visitor.Nodes;
    }

    private sealed class GetTopLevelDescendantVisitor<T> : TSqlFragmentVisitor
        where T : TSqlFragment
    {
        public List<T> Nodes { get; } = [];

        public override void Visit(TSqlFragment fragment)
        {
            if (fragment is T node)
            {
                Nodes.Add(node);
                return;
            }

            base.Visit(fragment);
        }
    }
}
