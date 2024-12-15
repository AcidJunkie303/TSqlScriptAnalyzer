using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public static partial class SqlHelpers
{
    public static IReadOnlyList<TSqlFragment> GetParents(TSqlFragment fragment)
    {
        var visitor = new ParentTrackingVisitor();
        fragment.Accept(visitor);

        return [];
    }

    private sealed class ParentTrackingVisitor : TSqlFragmentVisitor
    {
        private readonly Stack<TSqlFragment> _parentStack = new();

        public override void Visit(TSqlFragment fragment)
        {
            if (_parentStack.Count > 0)
            {
                Console.WriteLine($"Parent of {fragment.GetType().Name} is {_parentStack.Peek().GetType().Name}");
            }

            _parentStack.Push(fragment);

            // Continue traversal for child nodes
            base.Visit(fragment);

            _parentStack.Pop();
        }
    }
}
