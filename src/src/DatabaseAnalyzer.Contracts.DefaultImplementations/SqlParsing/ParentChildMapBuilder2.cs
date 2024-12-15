using System.Runtime.CompilerServices;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

// TODO: rename
public static class ParentChildMapBuilder2
{
    private static readonly ConditionalWeakTable<TSqlFragment, TSqlFragment?> ParentsByChild = [];
    private static readonly ConditionalWeakTable<TSqlFragment, List<TSqlFragment>?> ChildrenByParent = [];

    public static TSqlFragment? GetParent(this TSqlFragment fragment)
        => ParentsByChild.TryGetValue(fragment, out var parent) ? parent : null;

    public static IReadOnlyList<TSqlFragment> GetChildren(this TSqlFragment fragment)
    {
        ChildrenByParent.TryGetValue(fragment, out var children);
        return children ?? [];
    }

    public static void Build(TSqlScript script)
    {
        var visitor = new Visitor();
        visitor.ExplicitVisit(script);
    }

    private sealed class Visitor : TSqlConcreteFragmentVisitor
    {
        private readonly Stack<TSqlFragment> _stack = new();
        private readonly HashSet<TSqlFragment> _visited = [];

        public override void Visit(TSqlFragment fragment)
        {
            if (!_visited.Add(fragment))
            {
                return;
            }

            _stack.TryPeek(out var parent);
            _stack.Push(fragment);

            if (parent is not null)
            {
                if (!ChildrenByParent.TryGetValue(parent, out var children))
                {
                    children = [];
                    ChildrenByParent.Add(parent, children);
                }

                children!.Add(fragment);
            }

            ParentsByChild.Add(fragment, parent);

            fragment.AcceptChildren(this);

            _stack.Pop();
        }
    }
}
