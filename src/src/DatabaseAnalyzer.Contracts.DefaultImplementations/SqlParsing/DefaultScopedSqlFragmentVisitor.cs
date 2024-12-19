using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

// TODO: Remove
#pragma warning disable S125

public abstract class DefaultScopedSqlFragmentVisitor : ScopedSqlFragmentVisitor
{
    protected DefaultScopedSqlFragmentVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    public override void Visit(QuerySpecification node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        // Visit the FromClause first, if it exists
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

    public override void Visit(QualifiedJoin node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

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

    public override void Visit(FromClause node)
    {
        foreach (var namedTableReference in node.TableReferences.OfType<NamedTableReference>())
        {
            RegisterTableReference(namedTableReference);
        }

        base.Visit(node);

/*
        foreach (var tableReference in node.TableReferences.OfType<NamedTableReference>())
        {
            RegisterTableReference(tableReference);
        }
*/
        //     node.AcceptChildren(this);
    }

    public override void Visit(StatementWithCtesAndXmlNamespaces node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        using var scope = Scopes.BeginNewScope();

        node.AcceptChildren(this);
        //base.Visit(node);
    }

    private void RegisterTableReference(NamedTableReference node)
    {
        var alias = node.Alias?.Value;
        Scopes.CurrentScope.RegisterTableAlias(alias, node.SchemaObject, CurrentDatabaseName!, DefaultSchemaName);
    }

    /*
      base.AcceptChildren(visitor);
      if (this.TopRowFilter is not  null)
        this.TopRowFilter.Accept(visitor);
      int index = 0;
      for (int count = this.SelectElements.Count; index < count; ++index)
        this.SelectElements[index].Accept(visitor);
      if (this.FromClause is not  null)
        this.FromClause.Accept(visitor);
      if (this.WhereClause is not  null)
        this.WhereClause.Accept(visitor);
      if (this.GroupByClause is not  null)
        this.GroupByClause.Accept(visitor);
      if (this.HavingClause is not  null)
        this.HavingClause.Accept(visitor);
      if (this.WindowClause == null)
        return;
      this.WindowClause.Accept(visitor);
     */

    /*
     *      base.AcceptChildren(visitor);
      if (this.TopRowFilter is not  null)
        this.TopRowFilter.Accept(visitor);
      int index = 0;
      for (int count = this.SelectElements.Count; index < count; ++index)
        this.SelectElements[index].Accept(visitor);
      if (this.FromClause is not  null)
        this.FromClause.Accept(visitor);
      if (this.WhereClause is not  null)
        this.WhereClause.Accept(visitor);
      if (this.GroupByClause is not  null)
        this.GroupByClause.Accept(visitor);
      if (this.HavingClause is not  null)
        this.HavingClause.Accept(visitor);
      if (this.WindowClause == null)
        return;
      this.WindowClause.Accept(visitor);
     *
     */
}
