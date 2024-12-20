using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public class MyVisitor : TSqlFragmentVisitor
{
    private readonly List<(string ColumnName, string ObjectType)> _filteredColumns = [];

    public IReadOnlyList<(string ColumnName, string ObjectType)> FilteredColumns => _filteredColumns;

    public override void Visit(BooleanComparisonExpression node)
    {
        CollectColumnReferences(node, "table");
        base.Visit(node);
    }

    public override void Visit(BooleanBinaryExpression node)
    {
        CollectColumnReferences(node.FirstExpression, "table");
        CollectColumnReferences(node.SecondExpression, "table");
        base.Visit(node);
    }

    public override void Visit(QualifiedJoin node)
    {
        if (node.SearchCondition is BooleanExpression joinCondition)
        {
            CollectColumnReferences(joinCondition, "table");
        }

        base.Visit(node);
    }

    public override void Visit(CommonTableExpression node)
    {
        // Extract columns defined within the CTE and process them
        if (node.QueryExpression is QuerySpecification querySpecification)
        {
            foreach (var selectElement in querySpecification.SelectElements)
            {
                if (selectElement is SelectScalarExpression scalarExpression &&
                    scalarExpression.Expression is ColumnReferenceExpression columnReference)
                {
                    _filteredColumns.Add((GetFullColumnName(columnReference), "CTE"));
                }
            }
        }

        base.Visit(node);
    }

    private void CollectColumnReferences(TSqlFragment node, string objectType)
    {
        if (node is ColumnReferenceExpression columnReference)
        {
            // Collect schema.table.column or dbo.table.column when schema is not provided
            _filteredColumns.Add((GetFullColumnName(columnReference), objectType));
        }
        else if (node is BooleanBinaryExpression binaryExpression)
        {
            CollectColumnReferences(binaryExpression.FirstExpression, objectType);
            CollectColumnReferences(binaryExpression.SecondExpression, objectType);
        }
        else if (node is BooleanComparisonExpression comparisonExpression)
        {
            CollectColumnReferences(comparisonExpression.FirstExpression, objectType);
            CollectColumnReferences(comparisonExpression.SecondExpression, objectType);
        }
    }

    private static string GetFullColumnName(ColumnReferenceExpression columnReference)
    {
        if (columnReference.MultiPartIdentifier != null)
        {
            // MultiPartIdentifier contains Schema, Table, and Column parts
            var identifiers = columnReference.MultiPartIdentifier.Identifiers;

            // Default schema to "dbo" if not explicitly provided
            var schemaName = identifiers.Count == 3 ? identifiers[0].Value : "dbo";
            var tableName = identifiers.Count >= 2 ? identifiers[identifiers.Count - 2].Value : string.Empty;
            var columnName = identifiers.Count >= 1 ? identifiers[^1].Value : string.Empty;

            return $"{schemaName}.{tableName}.{columnName}";
        }

        return string.Empty;
    }
}
