using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction;
using DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing.Extraction.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class MissingIndexAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
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
                AnalyzeStatements(context, script, statementList, databasesByName);
            }
        }
    }

    private static void AnalyzeStatements(IAnalysisContext context, IScriptModel script, TSqlFragment fragment, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
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

            var fullObjectName = fragment.TryGetFirstClassObjectName(context.DefaultSchemaName, script.ParentFragmentProvider);
            context.IssueReporter.Report(DiagnosticDefinitions.Default,
                filteringColumn.DatabaseName,
                script.RelativeScriptFilePath,
                fullObjectName,
                filteringColumn.Fragment.GetCodeRegion(),
                filteringColumn.DatabaseName,
                filteringColumn.SchemaName,
                filteringColumn.TableName,
                filteringColumn.ColumnName,
                "script.sql",
                "Db1.dbo.Employee.Email"
            );
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5015",
            IssueType.MissingIndex,
            "Missing Index",
            "The column '{0}.{1}.{2}.{3}' defined in script '{4}' is not indexed but used as query filtering predicate in '{5}'."
        );
        /* Insertions Description:
            0 -> Database name
            1 -> Schema name
            2 -> Table name
            3 -> Column name
            4 -> Relative script file name containing the table creation statement
            5 -> Full object name or script file name (script file name is used in case object name is null)
        */
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
