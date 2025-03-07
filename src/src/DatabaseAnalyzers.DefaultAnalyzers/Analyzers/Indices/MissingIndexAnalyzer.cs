using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingIndexAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.FilteringColumnNotIndexed, DiagnosticDefinitions.ForeignKeyColumnNotIndexed];

    public void Analyze(IAnalysisContext context)
    {
        var databasesByName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.Scripts, context.DefaultSchemaName);

        AnalyzeModules(context, databasesByName);
        AnalyzeForeignKeys(context, databasesByName);
    }

    private static void AnalyzeForeignKeys(IAnalysisContext context, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5017Settings>();

        var tables = databasesByName
            .SelectMany(db => db.Value.SchemasByName.Values)
            .SelectMany(schema => schema.TablesByName.Values);

        foreach (var table in tables)
        {
            foreach (var foreignKey in table.ForeignKeys)
            {
                if (table.Indices.Any(index => index.ColumnNames.Contains(foreignKey.ColumnName)))
                {
                    continue;
                }

                if (settings.MissingIndexOnForeignKeyColumnSuppressions.Any(a => a.FullColumnNamePattern.IsMatch(foreignKey.FullColumnName)))
                {
                    continue;
                }

                var column = table.Columns.FirstOrDefault(a => a.ObjectName.EqualsOrdinalIgnoreCase(foreignKey.ColumnName));
                if (column is null)
                {
                    continue;
                }

                context.IssueReporter.Report(DiagnosticDefinitions.ForeignKeyColumnNotIndexed,
                    column.DatabaseName,
                    table.RelativeScriptFilePath,
                    table.FullName,
                    column.ColumnDefinition.GetCodeRegion(),
                    column.DatabaseName,
                    column.SchemaName,
                    column.TableName,
                    column.ObjectName
                );
            }
        }
    }

    private static void AnalyzeModules(IAnalysisContext context, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5015Settings>();

        foreach (var script in context.Scripts)
        {
            IEnumerable<StatementList?> statementLists =
            [
                .. script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true).Select(static a => a.StatementList),
                .. script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true).Select(static a => a.StatementList)
            ];

            foreach (var statementList in statementLists)
            {
                if (statementList is null)
                {
                    continue;
                }

                AnalyzeStatements(context, script, settings, statementList, databasesByName);
            }
        }
    }

    private static void AnalyzeStatements(IAnalysisContext context, IScriptModel script, Aj5015Settings settings, TSqlFragment fragment, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var finder = new FilteringColumnFinder(context.IssueReporter, script.ParsedScript, script.RelativeScriptFilePath, context.DefaultSchemaName, script.ParentFragmentProvider);

        foreach (var filteringColumn in finder.Find(fragment))
        {
            var table = databasesByName
                .GetValueOrDefault(filteringColumn.DatabaseName)
                ?.SchemasByName.GetValueOrDefault(filteringColumn.SchemaName)
                ?.TablesByName.GetValueOrDefault(filteringColumn.TableName);

            if (table is null)
            {
                continue;
            }

            if (table.ObjectName.IsTempTableName())
            {
                continue;
            }

            if (table.Indices.Any(a => a.ColumnNames.Contains(filteringColumn.ColumnName)))
            {
                return;
            }

            if (settings.MissingIndexSuppressions.Any(a => a.FullColumnNamePattern.IsMatch(filteringColumn.FullName)))
            {
                return;
            }

            var column = table.Columns.FirstOrDefault(a => a.ObjectName.EqualsOrdinalIgnoreCase(filteringColumn.ColumnName));
            var columnDefinitionCodeRegion = column is null
                ? CodeRegion.Unknown
                : column.ColumnDefinition.GetCodeRegion();

            var fullObjectName = fragment.TryGetFirstClassObjectName(context.DefaultSchemaName, script.ParsedScript, script.ParentFragmentProvider);
            context.IssueReporter.Report(DiagnosticDefinitions.FilteringColumnNotIndexed,
                filteringColumn.DatabaseName,
                script.RelativeScriptFilePath,
                fullObjectName,
                filteringColumn.Fragment.GetCodeRegion(),
                filteringColumn.DatabaseName,
                filteringColumn.SchemaName,
                filteringColumn.TableName,
                filteringColumn.ColumnName,
                table.RelativeScriptFilePath,
                columnDefinitionCodeRegion
            );
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition FilteringColumnNotIndexed { get; } = new
        (
            "AJ5015",
            IssueType.MissingIndex,
            "Missing Index",
            "The column `{0}.{1}.{2}.{3}` defined in script `{4}` at `{5}` is not indexed but used as column filtering predicate.",
            ["Database name", "Schema name", "Table name", "Column name", "Relative script file path of the table column declaration", "Code region of the table column declaration"],
            UrlPatterns.DefaultDiagnosticHelp
        );

        public static DiagnosticDefinition ForeignKeyColumnNotIndexed { get; } = new
        (
            "AJ5017",
            IssueType.MissingIndex,
            "Missing Index on foreign key column",
            "The foreign-key column `{0}.{1}.{2}.{3}` is not indexed. Although this columns might not be used for filtering directly, it is still recommended to create an index on it because it will improve performance when checking for referential integrity when deleting columns from the table being referenced for example.",
            ["Table name", "Schema name", "Table name", "Column name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
