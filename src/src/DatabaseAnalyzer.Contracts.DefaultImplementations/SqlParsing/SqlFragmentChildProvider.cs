using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static class SqlFragmentChildProvider
{
    public static IReadOnlyList<TSqlFragment> GetChildren(TSqlFragment fragment, bool recursive = false)
        => GetChildren<TSqlFragment>(fragment, recursive);

    public static IReadOnlyList<T> GetChildren<T>(TSqlFragment fragment, bool recursive = false)
        where T : TSqlFragment
    {
        var visitor = new RecursiveVisitor<T>(fragment, recursive);
        fragment.Accept(visitor);
        return visitor.Children;
    }

    private sealed class RecursiveVisitor<T> : TSqlFragmentVisitor
        where T : TSqlFragment
    {
        private readonly bool _isRecursive;
        private readonly TSqlFragment _root;
        private readonly HashSet<TSqlFragment> _visited = [];
        private int _currentDepth;

        public List<T> Children { get; } = [];

        public RecursiveVisitor(TSqlFragment root, bool isRecursive)
        {
            _root = root;
            _isRecursive = isRecursive;
        }

        public override void Visit(TSqlFragment fragment)
        {
            if (!_visited.Add(fragment))
            {
                return;
            }

            var isRoot = ReferenceEquals(fragment, _root);
            if (isRoot)
            {
                fragment.AcceptChildren(this);
                return;
            }

            if (!_isRecursive && (_currentDepth > 0))
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
