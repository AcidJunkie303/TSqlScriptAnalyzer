using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class InconsistentColumnDataTypeAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5054Settings>();
        var columnsAndScriptsByColumnName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.Scripts, context.DefaultSchemaName)
            .SelectMany(static a => a.Value.SchemasByName)
            .SelectMany(static a => a.Value.TablesByName.Values.Select(x => (Script: x.ScriptModel, Table: x)))
            .SelectMany(static a => a.Table.Columns.Select(x => (a.Script, a.Table, Column: x, DataType: x.ColumnDefinition.DataType.ToDataTypeString())))
            .GroupBy(static a => a.Column.ObjectName, StringComparer.OrdinalIgnoreCase)
            .Select(a => (ColumnName: a.Key, Columns: a));

        foreach (var columnData in columnsAndScriptsByColumnName)
        {
            var columnsAndScripts = columnData.Columns
                .Where(a => !settings.DatabasesToExclude.Contains(a.Column.DatabaseName))
                .Where(a => !settings.ColumnNamesToExclude.Contains(a.Column.ObjectName))
                .Where(a => !a.DataType.IsNullOrWhiteSpace())
                .ToList();

            if (columnsAndScripts.Count == 0)
            {
                continue;
            }

            var distinctDataTypeCount = columnsAndScripts.DistinctCount(a => a.DataType, StringComparer.OrdinalIgnoreCase);
            if (distinctDataTypeCount <= 1)
            {
                continue;
            }

            var dataTypeNames = columnsAndScripts
                .Select(a => a.DataType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .StringJoin(", ");

            var objectNames = columnsAndScripts
                .Select(a => a.Column.FullColumnName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .StringJoin(", ");

            var script = columnsAndScripts[0].Script;
            var column = columnsAndScripts[0].Column;

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(column.CreationStatement) ?? DatabaseNames.Unknown;
            var fullObjectName = column.CreationStatement.TryGetFirstClassObjectName(context, script);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, column.CreationStatement.GetCodeRegion(),
                column.ObjectName, dataTypeNames, objectNames);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5054",
            IssueType.Warning,
            "Inconsistent Column Data Type",
            "The column with name `{0}` has the following different data types `{1}` within the tables `{2}`.",
            ["Column Name", "Data Type Names", "Table Names"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
