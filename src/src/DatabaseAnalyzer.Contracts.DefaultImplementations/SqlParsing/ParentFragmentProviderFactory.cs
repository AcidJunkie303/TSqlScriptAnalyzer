using System.Collections.Frozen;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static class ParentFragmentProviderFactory
{
    public static IParentFragmentProvider Build(TSqlScript script)
    {
        var visitor = new Visitor();
        visitor.ExplicitVisit(script);

        return new Provider(script, visitor.ParentByChild.ToFrozenDictionary());
    }

    private sealed class Provider : IParentFragmentProvider
    {
        private readonly FrozenDictionary<TSqlFragment, TSqlFragment?> _parentByChild;

        public Provider(TSqlFragment root, FrozenDictionary<TSqlFragment, TSqlFragment?> parentByChild)
        {
            _parentByChild = parentByChild;
            Root = root;
        }

        public TSqlFragment Root { get; }
        public TSqlFragment? GetParent(TSqlFragment fragment) => _parentByChild.GetValueOrDefault(fragment);
    }

    private sealed class Visitor : TSqlConcreteFragmentVisitor
    {
        private readonly Stack<TSqlFragment> _stack = new();
        private readonly HashSet<TSqlFragment> _visited = [];

        public Dictionary<TSqlFragment, TSqlFragment?> ParentByChild { get; } = [];

        public override void Visit(TSqlFragment fragment)
        {
            if (!_visited.Add(fragment))
            {
                return;
            }

            _stack.TryPeek(out var parent);
            _stack.Push(fragment);

            ParentByChild[fragment] = parent;

            fragment.AcceptChildren(this);

            _stack.Pop();
        }
    }
}
