using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingIndexAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.FilteringColumnNotIndexed];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsRetriever.GetSettings<Aj5015Settings>();
        var databasesByName = new DatabaseObjectExtractor().Extract(context.Scripts, context.DefaultSchemaName);

        foreach (var script in context.Scripts)
        {
            IEnumerable<StatementList> statementLists =
            [
                .. script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true).Select(a => a.StatementList),
                .. script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true).Select(a => a.StatementList)
            ];

            foreach (var statementList in statementLists)
            {
                AnalyzeStatements(context, settings, script, statementList, databasesByName);
            }
        }
    }

    private static void AnalyzeStatements(IAnalysisContext context, Aj5015Settings settings, IScriptModel script, TSqlFragment fragment, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
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

            if (table.Indices.Any(a => a.ColumnNames.Contains(filteringColumn.ColumnName, StringComparer.OrdinalIgnoreCase)))
            {
                return;
            }

            if (settings.MissingIndexSuppressions.Any(a => a.FullColumnNamePattern.IsMatch(filteringColumn.FullName)))
            {
                return;
            }

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
                script.RelativeScriptFilePath
            );
        }
    }

    private static class DiagnosticDefinitions
    {
        /* Insertions Description:
            0 -> Database name
            1 -> Schema name
            2 -> Table name
            3 -> Column name
            4 -> Relative script file name containing the table creation statement
        */
        public static DiagnosticDefinition FilteringColumnNotIndexed { get; } = new
        (
            "AJ5015",
            IssueType.MissingIndex,
            "Missing Index",
            "The column '{0}.{1}.{2}.{3}' defined in script '{4}' is not indexed but used as column filtering predicate"
        );

        /* Insertions Description:
            0 -> Database name
            1 -> Schema name
            2 -> Table name
            3 -> Column name
            4 -> Relative script file name containing the table creation statement
        */
        public static DiagnosticDefinition ForeignKeyColumnNotIndexed { get; } = new
        (
            "AJ5017",
            IssueType.MissingIndex,
            "Missing Index",
            "The foreign-key column '{0}.{1}.{2}.{3}' defined in script '{4}' is not indexed. Although this columns might not be used for filtering directly, it is still recommended to create an index on it because it will improve performance checking for referential integrity when deleting columns from the table being referenced."
        );
    }

#pragma warning disable S125 // False positive
    /*
        required information about a missing index:
            - diagnostic id
            - issue type = MissingIndex
            - database name (of table)
            - schema name (of table)
            - table name
            - column name (in table)
            - table creation script path (relative)
            - Used By object (where the filtering was done (WHERE, join condition)
                - object database name
                - object schema name
                - object name
                - object type ( stored procedure, function, script)
                - relative file path of object where the filtering was done
                - code region (where the filtering was done)
        ultimately, we will group the missing index entities by db, schema, table and column so the report shows all locations where the filtering is done

        IIssue properties:
        - IDiagnosticDefinition DiagnosticDefinition { get; }
        - string RelativeScriptFilePath { get; }
        - string? ObjectName { get; }
        - CodeRegion CodeRegion { get; }
        - IReadOnlyList<string> MessageInsertionStrings { get; }
        - string Message { get; }
        - public string FullObjectNameOrFileName => ObjectName ?? Path.GetFileName(RelativeScriptFilePath) ?? "Unknown";
     */
#pragma warning restore S125
}
