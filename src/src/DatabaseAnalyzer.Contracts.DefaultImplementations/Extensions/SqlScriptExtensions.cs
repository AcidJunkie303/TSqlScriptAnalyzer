using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

public static class SqlScriptExtensions
{
    public static IParentFragmentProvider CreateParentFragmentProvider(this TSqlFragment fragment)
        => ParentFragmentProviderFactory.Build(fragment);

    public static IEnumerable<T> GetTopLevelDescendantsOfType<T>(this TSqlFragment fragment)
        where T : TSqlFragment
    {
        ArgumentNullException.ThrowIfNull(fragment);

        var visitor = new GetTopLevelDescendantVisitor<T>();
        fragment.Accept(visitor);
        return visitor.Nodes;
    }

    public static bool HasStatements(this TSqlScript script)
        => script
            .GetChildren(recursive: false)
            .Count > 0;

    public static bool IsUseDatabaseCalledBeforeAnyOtherStatement(this TSqlScript script)
        => ScriptComplianceChecker.IsUseDatabaseCalledBeforeAnyOtherStatement(script);

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
