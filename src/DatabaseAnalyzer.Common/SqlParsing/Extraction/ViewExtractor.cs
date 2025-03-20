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
        var columnNames = statement.Columns
            .Select(a => a.Value)
            .ToList();

        return new ViewInformation
        (
            DatabaseName: calculatedDatabaseName,
            SchemaName: viewSchemaName,
            ObjectName: viewName,
            Columns: columnNames,
            CreationStatement: statement,
            RelativeScriptFilePath: script.RelativeScriptFilePath
        )
        {
            ScriptModel = script
        };
    }
}
