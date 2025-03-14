using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods")]
public abstract class DatabaseAwareFragmentVisitor : TSqlFragmentVisitor
{
    private readonly HashSet<TSqlFragment> _visitedNodes = [];

    protected DatabaseAwareFragmentVisitor(string defaultSchemaName)
    {
        DefaultSchemaName = defaultSchemaName;
    }

    protected string DefaultSchemaName { get; }
    protected string? CurrentDatabaseName { get; set; }

    protected bool TrackNodeAndCheck(TSqlFragment node) => _visitedNodes.Add(node);
    protected bool IsNodeTracked(TSqlFragment node) => _visitedNodes.Contains(node);

    public override void ExplicitVisit(UseStatement node)
    {
        CurrentDatabaseName = node.DatabaseName.Value;
        base.ExplicitVisit(node);
    }
}
