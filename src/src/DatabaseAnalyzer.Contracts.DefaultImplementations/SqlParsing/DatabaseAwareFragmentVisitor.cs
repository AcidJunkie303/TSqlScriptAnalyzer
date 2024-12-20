using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

// TODO: remove
#pragma warning disable S125

public abstract class DatabaseAwareFragmentVisitor : TSqlFragmentVisitor //: TrackingSqlFragmentVisitor
{
    private readonly HashSet<TSqlFragment> _visitedExplicitNodes = [];
    private readonly HashSet<TSqlFragment> _visitedNodes = [];
    protected string DefaultSchemaName { get; }
    protected string? CurrentDatabaseName { get; set; }

    protected DatabaseAwareFragmentVisitor(string defaultSchemaName)
    {
        DefaultSchemaName = defaultSchemaName;
    }

    protected bool TrackNodeAndCheck(TSqlFragment node) => _visitedNodes.Add(node);
    protected bool IsNodeTracked(TSqlFragment node) => _visitedNodes.Contains(node);
    protected bool TrackExplicitNodeAndCheck(TSqlFragment node) => _visitedExplicitNodes.Add(node);

    public override void ExplicitVisit(UseStatement node)
    {
        CurrentDatabaseName = node.DatabaseName.Value;
        base.ExplicitVisit(node);
    }
}
