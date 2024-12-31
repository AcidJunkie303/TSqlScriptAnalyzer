using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static class SqlFragmentChildrenProvider
{
    public static IReadOnlyList<TSqlFragment> GetChildren(TSqlFragment fragment, bool recursive = false, Func<TSqlFragment, bool>? continueBranchRecursionPredicate = null)
        => GetChildren<TSqlFragment>(fragment, recursive, continueBranchRecursionPredicate);

    public static IReadOnlyList<T> GetChildren<T>(TSqlFragment fragment, bool recursive = false, Func<TSqlFragment, bool>? continueBranchRecursionPredicate = null)
        where T : TSqlFragment
    {
        ArgumentNullException.ThrowIfNull(fragment);

        var visitor = new RecursiveVisitor<T>(fragment, recursive, continueBranchRecursionPredicate);
        fragment.Accept(visitor);
        return visitor.Children;
    }

    private sealed class RecursiveVisitor<T> : TSqlFragmentVisitor
        where T : TSqlFragment
    {
        private readonly Func<TSqlFragment, bool> _continueBranchRecursionPredicate;
        private readonly bool _isRecursive;
        private readonly TSqlFragment _root;
        private readonly HashSet<TSqlFragment> _visited = [];
        private int _currentDepth;

        public RecursiveVisitor(TSqlFragment root, bool isRecursive, Func<TSqlFragment, bool>? continueBranchRecursionPredicate)
        {
            _root = root;
            _isRecursive = isRecursive;
            _continueBranchRecursionPredicate = continueBranchRecursionPredicate ?? (static _ => true);
        }

        public List<T> Children { get; } = [];

        public override void Visit(TSqlFragment fragment)
        {
            if (!_visited.Add(fragment))
            {
                return;
            }

            if (!_continueBranchRecursionPredicate(fragment))
            {
                return;
            }

            var isRoot = ReferenceEquals(fragment, _root);
            if (isRoot)
            {
                fragment.AcceptChildren(this);
                return;
            }

            if (!_isRecursive && _currentDepth > 0)
            {
                return;
            }

            _currentDepth++;

            if (fragment is T typedFragment)
            {
                Children.Add(typedFragment);
            }

            fragment.AcceptChildren(this);

            _currentDepth--;
        }
    }
}
