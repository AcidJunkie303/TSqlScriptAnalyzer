using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Common.Various;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class UnusedIndexAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5051Settings>();

        var filteringColumnsByName = GetFilteringColumns(context)
            .Select(col => (Key: new Key(col.DatabaseName, col.SchemaName, col.TableName, col.ColumnName), Column: col))
            .GroupBy(a => a.Key)
            .ToDictionary(a => a.Key, a => a.ToImmutableArray())
            .AsIReadOnlyDictionary();

        var databasesByName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.ErrorFreeScripts, context.DefaultSchemaName);

        var allIndices = databasesByName.Values
            .SelectMany(db => db.SchemasByName.Values)
            .SelectMany(schema => schema.TablesByName.Values)
            .SelectMany(table => table.Indices);

        foreach (var index in allIndices)
        {
            foreach (var column in index.ColumnNames)
            {
                if (settings.IgnoreUnusedPrimaryKeyIndices && index.IndexType.HasFlag(TableColumnIndexTypes.PrimaryKey))
                {
                    continue;
                }

                var key = new Key(index.DatabaseName, index.SchemaName, index.TableName, column);
                if (filteringColumnsByName.ContainsKey(key))
                {
                    continue;
                }

                context.IssueReporter.Report(DiagnosticDefinitions.Default, index.DatabaseName, index.RelativeScriptFilePath, index.IndexName, index.CreationStatement.GetCodeRegion(),
                    index.DatabaseName, index.SchemaName, index.TableName, column, index.IndexName ?? Constants.UnknownObjectName);
            }
        }
    }

    private static IEnumerable<ColumnReference> GetFilteringColumns(IAnalysisContext context)
    {
        foreach (var script in context.ErrorFreeScripts)
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

                foreach (var filteringColumn in GetFilteringColumnFromStatement(context, script, statementList))
                {
                    yield return filteringColumn;
                }
            }
        }
    }

    private static IEnumerable<ColumnReference> GetFilteringColumnFromStatement(IAnalysisContext context, IScriptModel script, TSqlFragment fragment)
    {
        var finder = new FilteringColumnFinder(context.IssueReporter, script.ParsedScript, script.RelativeScriptFilePath, context.DefaultSchemaName, script.ParentFragmentProvider);

        foreach (var filteringColumn in finder.Find(fragment))
        {
            if (filteringColumn.SourceType == TableSourceType.TableOrView)
            {
                continue;
            }

            yield return filteringColumn;
        }
    }

    private sealed record Key(CaseInsensitiveString DatabaseName, CaseInsensitiveString SchemaName, CaseInsensitiveString TableName, CaseInsensitiveString ColumnName);

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5051",
            IssueType.Warning,
            "Unused Index",
            "The column `{0}.{1}.{2}.{3}` is part of the index `{4}` but none of the scripts seems to use it as a filtering predicate.",
            ["Database name", "Schema name", "Table name", "Column name", "Index Name"],
            UrlPatterns.DefaultDiagnosticHelp
        );
    }
}
