using System.Collections.Frozen;
using DatabaseAnalyzer.Common.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

public static class ParentFragmentProviderFactory
{
    public static IParentFragmentProvider Build(TSqlFragment root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var visitor = new Visitor();
        root.Accept(visitor);

        return new Provider(root, visitor.ParentByChild);
    }

    private sealed class Provider : IParentFragmentProvider
    {
        private readonly FrozenDictionary<TSqlFragment, TSqlFragment?> _parentByChild;

        public TSqlFragment Root { get; }

        public Provider(TSqlFragment root, FrozenDictionary<TSqlFragment, TSqlFragment?> parentByChild)
        {
            _parentByChild = parentByChild;
            Root = root;
        }

        public TSqlFragment? GetParent(TSqlFragment fragment) => _parentByChild.GetValueOrDefault(fragment);
    }

    private sealed class Visitor : TSqlConcreteFragmentVisitor
    {
        private readonly Dictionary<TSqlFragment, TSqlFragment?> _parentByChild = [];
        private readonly Stack<TSqlFragment> _stack = new();
        private readonly HashSet<TSqlFragment> _visited = [];

        public FrozenDictionary<TSqlFragment, TSqlFragment?> ParentByChild => _parentByChild.ToFrozenDictionary();

        public override void Visit(TSqlFragment fragment)
        {
            if (!_visited.Add(fragment))
            {
                return;
            }

            _stack.TryPeek(out var parent);
            _stack.Push(fragment);

            _parentByChild[fragment] = parent;

            fragment.AcceptChildren(this);

            _stack.Pop();
        }
    }
}
