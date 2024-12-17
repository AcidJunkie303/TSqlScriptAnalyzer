using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;

internal abstract class DatabaseAwareFragmentVisitor : TSqlConcreteFragmentVisitor
{
    protected string? CurrentDatabaseName { get; set; }

    public override void Visit(UseStatement node)
    {
        CurrentDatabaseName = node.DatabaseName.Value;
        base.Visit(node);
    }
}
