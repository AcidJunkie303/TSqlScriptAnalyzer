using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public abstract class DatabaseAwareFragmentVisitor : TSqlFragmentVisitor //: TrackingSqlFragmentVisitor
{
    private readonly HashSet<TSqlFragment> _visitedNodes = [];
    protected string DefaultSchemaName { get; }
    protected string? CurrentDatabaseName { get; set; }

    protected DatabaseAwareFragmentVisitor(string defaultSchemaName)
    {
        DefaultSchemaName = defaultSchemaName;
    }

    protected bool TrackNodeAndCheck(TSqlFragment node) => _visitedNodes.Add(node);

    public override void Visit(UseStatement node)
    {
        CurrentDatabaseName = node.DatabaseName.Value;
        base.Visit(node);
    }
}
