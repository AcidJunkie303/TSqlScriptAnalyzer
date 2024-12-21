using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

internal sealed class FilteringColumnExpressionFinder : DatabaseAwareFragmentVisitor
{
    private readonly List<(string DatabaseName, ColumnReferenceExpression Column)> _columns = [];
    private readonly IParentFragmentProvider _parentFragmentProvider;

    public IReadOnlyList<(string DatabaseName, ColumnReferenceExpression Column)> Columns => _columns;

    public FilteringColumnExpressionFinder(string defaultSchemaName, TSqlScript script) : this(defaultSchemaName, script.CreateParentFragmentProvider())
    {
    }

    public FilteringColumnExpressionFinder(string defaultSchemaName, IParentFragmentProvider parentFragmentProvider) : base(defaultSchemaName)
    {
        _parentFragmentProvider = parentFragmentProvider;
    }

    public override void Visit(ColumnReferenceExpression node)
    {
        if (IsImmediateParentComparison(node))
        {
            _columns.Add((CurrentDatabaseName!, node));
        }

        base.Visit(node);

        bool IsImmediateParentComparison(TSqlFragment fragment)
        {
            var parent = fragment.GetParent(_parentFragmentProvider);
            if (parent is FunctionCall)
            {
                return IsImmediateParentComparison(parent);
            }

            return parent is BooleanComparisonExpression or InPredicate;
        }
    }
}

[SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together")]
internal sealed class FilteringColumnVisitor : DefaultScopedSqlFragmentVisitor
{
    private readonly List<FilteringColumn> _filteringColumns = [];
    public IReadOnlyList<FilteringColumn> FilteringColumns => _filteringColumns;

    public FilteringColumnVisitor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    public override void Visit(TSqlFragment fragment)
    {
        if (IsNodeTracked(fragment))
        {
            return;
        }

        base.Visit(fragment);
    }

    public override void ExplicitVisit(QualifiedJoin node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        base.ExplicitVisit(node);

        if (node.SearchCondition is BooleanComparisonExpression booleanComparisonExpression)
        {
            ExtractFilteringColumn(booleanComparisonExpression.FirstExpression, booleanComparisonExpression.SecondExpression);
        }
    }

    public override void ExplicitVisit(CommonTableExpression node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        Scopes.CurrentScope.RegisterCommonTableExpressionName(node.ExpressionName.Value);
        Scopes.CurrentScope.RegisterTableAlias(null, CurrentDatabaseName!, DefaultSchemaName, node.ExpressionName.Value, SourceType.Cte);

        using var scope = Scopes.BeginNewScope(node);

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(WithCtesAndXmlNamespaces node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(SelectStatement node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        using var scope = Scopes.BeginNewScope(node);

        if (node.WithCtesAndXmlNamespaces is not null && TrackNodeAndCheck(node.WithCtesAndXmlNamespaces))
        {
            foreach (var cte in node.WithCtesAndXmlNamespaces?.CommonTableExpressions ?? [])
            {
                ExplicitVisit(cte);
            }
        }

        if (node.Into is not null)
        {
            if ((node.Into.Identifiers.Count == 1) && node.Into.Identifiers[0].Value.StartsWith('#'))
            {
                // since temp tables have a wider scope (session), we need to register it in the root scope
                Scopes.RootScope.RegisterTableAlias(null, CurrentDatabaseName!, DefaultSchemaName, node.Into.Identifiers[0].Value, SourceType.TempTable);
                Scopes.CurrentScope.RegisterTableAlias(null, CurrentDatabaseName!, DefaultSchemaName, node.Into.Identifiers[0].Value, SourceType.TempTable);
            }
        }

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(FromClause node)
    {
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(InPredicate node)
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

        base.ExplicitVisit(node);
    }

    public override void ExplicitVisit(BooleanComparisonExpression node)
    {
        base.ExplicitVisit(node);
        if (!TrackNodeAndCheck(node))
        {
            return;
        }

        ExtractFilteringColumn(node.FirstExpression, node.SecondExpression);
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
            {
                var allTablesAndAliases = Scopes.CurrentScope.TableReferencesByFullNameOrAlias.Values.ToList();
                if (allTablesAndAliases.Count > 1)
                {
                    // TODO:    Raise diagnostic because we cannot be sure for which table the columns refers to without having the DB schema
                    //          Furthermore, it's also easier to read/understand for human beings ;)
                    return null;
                }

                var tableAndAlias = allTablesAndAliases.FirstOrDefault();
                var tableName = tableAndAlias?.TableName;
                var sourceType = tableAndAlias?.SourceType ?? SourceType.Other;

                return new FilteringColumn(CurrentDatabaseName!, DefaultSchemaName, tableName, identifiers[0].Value, sourceType, expression);
            }

            case 2: // alias.column or table.column -> need to lookup for alias first
            {
                var aliasOrSchema = identifiers[0].Value;
                var columnName = identifiers[1].Value;
                var tableAndAlias = Scopes.FindTableByAlias(aliasOrSchema);
                var sourceType = tableAndAlias?.SourceType ?? SourceType.Other;
                return tableAndAlias is null
                    ? new FilteringColumn(CurrentDatabaseName!, DefaultSchemaName, null, columnName, sourceType, expression)
                    : new FilteringColumn(tableAndAlias.DatabaseName, tableAndAlias.SchemaName, tableAndAlias.TableName, columnName, sourceType, expression);
            }

            case 3: // schema.table.column
            {
                return new FilteringColumn(CurrentDatabaseName!, identifiers[0].Value, identifiers[1].Value, identifiers[2].Value, SourceType.TableOrView, expression);
            }

            case 4: // database.schema.table.column
            {
                return new FilteringColumn(identifiers[0].Value, identifiers[1].Value, identifiers[2].Value, identifiers[3].Value, SourceType.TableOrView, expression);
            }

            default:
                return null;
        }
    }

    internal sealed record FilteringColumn(string DatabaseName, string SchemaName, string? TableName, string ColumnName, SourceType SourceType, TSqlFragment Fragment)
    {
        public override string ToString() => $"{SourceType}: {DatabaseName}.{SchemaName}.{TableName}.{ColumnName} at {(Fragment is null ? "NULL" : Fragment.GetCodeRegion().ToString())}";
    }
}
