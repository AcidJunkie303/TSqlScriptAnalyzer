using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public sealed class ParentChildMapBuilder
{
    public static ParentChildMap Build(TSqlScript script)
    {
        var visitor = new Visitor();
        visitor.ExplicitVisit(script);

        var childrenByParent = visitor.ChildrenByParent
            .ToFrozenDictionary(
                a => a.Key,
                a => (IReadOnlyList<TSqlFragment>)a.Value.ToImmutableArray());
        var parentByChild = visitor.ParentByChild.ToFrozenDictionary();

        return new ParentChildMap(
            childrenByParent,
            parentByChild,
            parentByChild.Single(a => a.Value is null).Key
        );
    }

    private sealed class Visitor : TSqlConcreteFragmentVisitor
    {
        private readonly Stack<TSqlFragment> _stack = new();
        private readonly HashSet<TSqlFragment> _visited = [];
        public Dictionary<TSqlFragment, List<TSqlFragment>> ChildrenByParent { get; } = [];
        public Dictionary<TSqlFragment, TSqlFragment?> ParentByChild { get; } = [];

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
                    ChildrenByParent[parent] = children;
                }

                children.Add(fragment);
            }

            ParentByChild[fragment] = parent;

            fragment.AcceptChildren(this);

            _stack.Pop();
        }
    }
}
