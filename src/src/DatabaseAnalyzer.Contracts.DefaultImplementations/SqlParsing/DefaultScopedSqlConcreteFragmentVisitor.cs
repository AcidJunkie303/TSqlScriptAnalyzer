using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

// TODO: Remove
#pragma warning disable S125

public abstract class DefaultScopedSqlConcreteFragmentVisitor : ScopedSqlConcreteFragmentVisitor
{
    protected DefaultScopedSqlConcreteFragmentVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    public override void Visit(QuerySpecification node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        // Visit the FromClause first, if it exists
        if (node.FromClause is not null && TrackNodeAndCheck(node.FromClause))
        {
            node.FromClause.Accept(this);
        }

        node.WhereClause?.Accept(this);

        foreach (var selectElement in node.SelectElements)
        {
            selectElement.Accept(this);
        }

        node.GroupByClause?.Accept(this);
        node.HavingClause?.Accept(this);

        //base.ExplicitVisit(node); // Optionally call the base method
    }

    public override void Visit(QualifiedJoin node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        if (node.FirstTableReference is NamedTableReference firstTable)
        {
            var alias = firstTable.Alias?.Value;
            Scopes.CurrentScope.RegisterTableAlias(alias, firstTable.SchemaObject, CurrentDatabaseName!, DefaultSchemaName);
        }

        if (node.SecondTableReference is NamedTableReference secondTable)
        {
            var alias = secondTable.Alias?.Value;
            Scopes.CurrentScope.RegisterTableAlias(alias, secondTable.SchemaObject, CurrentDatabaseName!, DefaultSchemaName);
        }

        //base.ExplicitVisit(node); // Visit children of FromClause
    }

    public override void Visit(SelectStatement node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        using var scope = Scopes.BeginNewScope();

        node.AcceptChildren(this);

        Console.WriteLine();
    }

    public override void Visit(CommonTableExpression node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        using var scope = Scopes.BeginNewScope();

        base.Visit(node);

        Console.WriteLine();
        Console.WriteLine();
    }

    public override void ExplicitVisit(CommonTableExpression node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        using var scope = Scopes.BeginNewScope();

        base.Visit(node);

        Console.WriteLine();
    }
}
