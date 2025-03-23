using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Common.SqlParsing.Extraction;

public sealed class ViewExtractor : Extractor<ViewInformation>
{
    public ViewExtractor(string defaultSchemaName) : base(defaultSchemaName)
    {
    }

    protected override List<ViewInformation> ExtractCore(IScriptModel script)
    {
        var visitor = new ObjectExtractorVisitor<ViewStatementBody>(DefaultSchemaName);
        script.ParsedScript.AcceptChildren(visitor);

        return visitor.Objects.ConvertAll(a => GetView(a.Object, a.DatabaseName, script));
    }

    private ViewInformation GetView(ViewStatementBody statement, string? databaseName, IScriptModel script)
    {
        var viewSchemaName = statement.SchemaObjectName.SchemaIdentifier?.Value ?? DefaultSchemaName;
        var viewName = statement.SchemaObjectName.BaseIdentifier.Value!;
        var calculatedDatabaseName = statement.SchemaObjectName.DatabaseIdentifier?.Value ?? databaseName ?? throw CreateUnableToDetermineTheDatabaseNameException("view", $"{viewSchemaName}.{viewName}", statement.GetCodeRegion());
        var columns = GetColumns();

        return new ViewInformation
        (
            DatabaseName: calculatedDatabaseName,
            SchemaName: viewSchemaName,
            ObjectName: viewName,
            Columns: columns,
            CreationStatement: statement,
            RelativeScriptFilePath: script.RelativeScriptFilePath
        )
        {
            ScriptModel = script
        };

        IReadOnlyList<ViewColumnInformation> GetColumns()
        {
            if (statement.SelectStatement.QueryExpression is not QuerySpecification querySpecification)
            {
                return [];
            }

            return querySpecification.SelectElements
                .OfType<SelectScalarExpression>()
                .Select(a => (Statement: a, Name: GetColumnName(a)))
                .Where(a => !a.Name.IsNullOrWhiteSpace())
                .Select(a => new ViewColumnInformation(calculatedDatabaseName, viewSchemaName, viewName, a.Name!, a.Statement, script.RelativeScriptFilePath))
                .ToList();
        }
    }

    private static string? GetColumnName(SelectScalarExpression expression)
    {
        if (expression.ColumnName?.Value is not null)
        {
            return expression.ColumnName.Value;
        }

        if (expression.Expression is ColumnReferenceExpression columnReferenceExpression)
        {
            return (columnReferenceExpression.MultiPartIdentifier?.Identifiers).IsNullOrEmpty()
                ? null
                : columnReferenceExpression.MultiPartIdentifier?.Identifiers[^1].Value;
        }

        return null;
    }
}
