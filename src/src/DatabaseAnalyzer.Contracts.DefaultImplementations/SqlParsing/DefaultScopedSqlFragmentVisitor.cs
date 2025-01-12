using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public abstract class DefaultScopedSqlFragmentVisitor : ScopedSqlFragmentVisitor
{
    protected DefaultScopedSqlFragmentVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    public override void ExplicitVisit(QuerySpecification node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        // Visit the FromClause first, if it exists
        // this is necessary because we need to know the source table first before diving into the other parts
        node.FromClause?.Accept(this);

        node.WhereClause?.Accept(this);

        foreach (var selectElement in node.SelectElements)
        {
            selectElement.Accept(this);
        }

        node.GroupByClause?.Accept(this);
        node.HavingClause?.Accept(this);

        //base.ExplicitVisit(node); // Optionally call the base method
    }

    public override void ExplicitVisit(QualifiedJoin node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.FirstTableReference is NamedTableReference firstTable && TrackNodeAndCheck(firstTable))
        {
            RegisterTableReference(firstTable);
        }

        if (node.SecondTableReference is NamedTableReference secondTable && TrackNodeAndCheck(secondTable))
        {
            RegisterTableReference(secondTable);
        }

        //base.ExplicitVisit(node); // Visit children of FromClause
    }

    public override void ExplicitVisit(FromClause node)
    {
        ArgumentNullException.ThrowIfNull(node);
        foreach (var namedTableReference in node.TableReferences.OfType<NamedTableReference>())
        {
            RegisterTableReference(namedTableReference);
        }

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(InsertStatement node)
    {
        using var scope = Scopes.BeginNewScope(node);

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(UpdateStatement node)
    {
        using var scope = Scopes.BeginNewScope(node);

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(DeleteStatement node)
    {
        using var scope = Scopes.BeginNewScope(node);

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(SelectStatement node)
    {
        using var scope = Scopes.BeginNewScope(node);

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(StatementWithCtesAndXmlNamespaces node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        using var scope = Scopes.BeginNewScope(node);

        node.AcceptChildren(this);
    }

    private void RegisterTableReference(NamedTableReference node)
    {
        var sourceType = SourceType.TableOrView;
        if (node.SchemaObject.Identifiers.Count == 1) // maybe it's a CTE or a temp table
        {
            var objectName = node.SchemaObject.Identifiers[0].Value;
            if (objectName.StartsWith('#'))
            {
                sourceType = SourceType.TempTable;
            }
            else if (Scopes.IsCommonTableExpressionName(objectName))
            {
                sourceType = SourceType.Cte;
            }
        }

        var alias = node.Alias?.Value;
        Scopes.CurrentScope.RegisterTableAlias(alias, node.SchemaObject, CurrentDatabaseName!, DefaultSchemaName, sourceType);
    }
}
