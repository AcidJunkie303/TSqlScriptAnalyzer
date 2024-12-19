using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

// TODO: remove
#pragma warning disable S125

internal sealed class FilteringColumnVisitor : DefaultScopedSqlFragmentVisitor
{
    private readonly List<FilteringColumn> _filteringColumns = [];
    public IReadOnlyList<FilteringColumn> FilteringColumns => _filteringColumns;

    public FilteringColumnVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    public override void Visit(SelectStatement node)
    {
        Console.WriteLine();
        base.Visit(node);
    }

    public override void Visit(FromClause node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        base.Visit(node);
    }

    public override void Visit(InPredicate node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        if (node.Expression is ColumnReferenceExpression columnReference)
        {
            var filteringColumn = TryGetFilteringColumn(columnReference);
            if (filteringColumn is not null)
            {
                _filteringColumns.Add(filteringColumn);
            }
        }

        base.Visit(node);
    }

    public override void Visit(BooleanComparisonExpression node)
    {
        base.Visit(node);
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        ExtractFilteringColumn(node.FirstExpression, node.SecondExpression);

        /*
|         WhereClause                       | (8,1)-(8,36)      | WHERE               Name = N'Hello'                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
|           BooleanComparisonExpression     | (8,21)-(8,36)     | Name = N'Hello'                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
|             ColumnReferenceExpression     | (8,21)-(8,25)     | Name                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|               MultiPartIdentifier         | (8,21)-(8,25)     | Name                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|                 Identifier                | (8,21)-(8,25)     | Name                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|             StringLiteral                 | (8,28)-(8,36)     | N'Hello'

OR

|             BooleanComparisonExpression   | (9,46)-(9,77)     | p.DepartmentId = d.DepartmentId                                                                                                                                                                                                                                                                                                                   |
|               ColumnReferenceExpression   | (9,46)-(9,60)     | p.DepartmentId                                                                                                                                                                                                                                                                                                                                    |
|                 MultiPartIdentifier       | (9,46)-(9,60)     | p.DepartmentId                                                                                                                                                                                                                                                                                                                                    |
|                   Identifier              | (9,46)-(9,47)     | p                                                                                                                                                                                                                                                                                                                                                 |
|                   Identifier              | (9,48)-(9,60)     | DepartmentId                                                                                                                                                                                                                                                                                                                                      |
|               ColumnReferenceExpression   | (9,63)-(9,77)     | d.DepartmentId                                                                                                                                                                                                                                                                                                                                    |
|                 MultiPartIdentifier       | (9,63)-(9,77)     | d.DepartmentId                                                                                                                                                                                                                                                                                                                                    |
|                   Identifier              | (9,63)-(9,64)     | d                                                                                                                                                                                                                                                                                                                                                 |
|                   Identifier              | (9,65)-(9,77)     | DepartmentId
         */
    }

    private void ExtractFilteringColumn(ScalarExpression firstExpression, ScalarExpression secondExpression)
    {
        var firstFilteringColumn = TryGetFilteringColumn(firstExpression);
        var secondFilteringColumn = TryGetFilteringColumn(secondExpression);

        _filteringColumns.AddIfNotNull(firstFilteringColumn);
        _filteringColumns.AddIfNotNull(secondFilteringColumn);
    }

    private FilteringColumn? TryGetFilteringColumn(ScalarExpression expression)
    {
        if (expression is not ColumnReferenceExpression columnReferenceExpression)
        {
            return null;
        }

        var identifiers = columnReferenceExpression.MultiPartIdentifier.Identifiers;
        switch (identifiers.Count)
        {
            case 1: // column
                var allTablesAndAliases = Scopes.AllTableAndAliases.ToList();

                if (allTablesAndAliases.Count > 1)
                {
                    // TODO: raise error because we cannot be sure for which table the columns refers to without having the DB schema
                    return null;
                }

                var tableName = allTablesAndAliases.FirstOrDefault()?.TableName;
                return new FilteringColumn(CurrentDatabaseName!, DefaultSchemaName, tableName, identifiers[0].Value, expression);

            case 2: // alias.column or table.column -> need to lookup for alias first
                var aliasOrSchema = identifiers[0].Value;
                var columnName = identifiers[1].Value;
                var tableAndAlias = Scopes.FindTableByAlias(aliasOrSchema);
                return tableAndAlias is null
                    ? new FilteringColumn(CurrentDatabaseName!, DefaultSchemaName, null, columnName, expression)
                    : new FilteringColumn(tableAndAlias.DatabaseName, tableAndAlias.SchemaName, tableAndAlias.TableName, columnName, expression);

            case 3: // schema.table.column
                return new FilteringColumn(CurrentDatabaseName!, identifiers[0].Value, identifiers[1].Value, identifiers[2].Value, expression);

            case 4: // database.schema.table.column
                return new FilteringColumn(identifiers[0].Value, identifiers[1].Value, identifiers[2].Value, identifiers[3].Value, expression);

            default:
                return null;
        }
    }

    internal sealed record FilteringColumn(string DatabaseName, string SchemaName, string? TableName, string ColumnName, TSqlFragment Fragment);
/*
    public override void Visit(QualifiedJoin node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        base.Visit(node);

        if (node.SearchCondition is BooleanComparisonExpression booleanComparisonExpression)
        {
            ExtractFilteringColumn(booleanComparisonExpression.FirstExpression, booleanComparisonExpression.SecondExpression);
        }
    }
*/
}
