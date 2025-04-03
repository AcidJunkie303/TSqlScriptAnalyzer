using System.Collections.Concurrent;
using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Models;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.Various;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class UnusedIndexAnalyzer : IGlobalAnalyzer
{
    private readonly IAstService _astService;
    private readonly IGlobalAnalysisContext _context;
    private readonly IIssueReporter _issueReporter;
    private readonly IObjectProvider _objectProvider;
    private readonly ParallelOptions _parallelOptions;
    private readonly Aj5051Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public UnusedIndexAnalyzer(IGlobalAnalysisContext context, IIssueReporter issueReporter, Aj5051Settings settings, IAstService astService, IObjectProvider objectProvider, ParallelOptions parallelOptions)
    {
        _context = context;
        _issueReporter = issueReporter;
        _settings = settings;
        _astService = astService;
        _objectProvider = objectProvider;
        _parallelOptions = parallelOptions;
    }

    public void Analyze()
    {
        var filteringColumnsByName = GetFilteringColumns()
            .Select(col => (Key: new Key(col.DatabaseName, col.SchemaName, col.TableName, col.ColumnName), Column: col))
            .GroupBy(a => a.Key)
            .ToDictionary(a => a.Key, a => a.ToImmutableArray())
            .AsIReadOnlyDictionary();

        var allIndices = _objectProvider.DatabasesByName.Values
            .SelectMany(db => db.SchemasByName.Values)
            .SelectMany(schema => schema.TablesByName.Values)
            .SelectMany(table => table.Indices);

        Parallel.ForEach(allIndices, _parallelOptions, index =>
        {
            foreach (var columnName in index.ColumnNames)
            {
                if (_settings.IgnoreUnusedPrimaryKeyIndices && index.IndexType.HasFlag(TableColumnIndexTypes.PrimaryKey))
                {
                    continue;
                }

                var key = new Key(index.DatabaseName, index.SchemaName, index.TableName, columnName);
                if (filteringColumnsByName.ContainsKey(key))
                {
                    continue;
                }

                var table = _objectProvider.GetTable(index.DatabaseName, index.SchemaName, index.TableName);
                if (table?.ForeignKeysByColumnName.ContainsKey(columnName) == true)
                {
                    continue;
                }

                _issueReporter.Report(DiagnosticDefinitions.Default, index.DatabaseName, index.RelativeScriptFilePath, index.IndexName, index.CreationStatement.GetCodeRegion(),
                    index.DatabaseName, index.SchemaName, index.TableName, columnName, index.IndexName ?? Constants.UnknownObjectName);
            }
        });
    }

    private List<ColumnReference> GetFilteringColumns()
    {
        // this is pretty performance hungry. That's why we nest another Parallel.Foreach
        var result = new ConcurrentBag<ColumnReference>();

        Parallel.ForEach(_context.ErrorFreeScripts, _parallelOptions, script =>
        {
            IEnumerable<StatementList?> statementLists =
            [
                .. script.ParsedScript.GetChildren<ProcedureStatementBody>(recursive: true).Select(static a => a.StatementList),
                .. script.ParsedScript.GetChildren<FunctionStatementBody>(recursive: true).Select(static a => a.StatementList)
            ];

            Parallel.ForEach(statementLists, _parallelOptions, statementList =>
            {
                if (statementList is null)
                {
                    return;
                }

                foreach (var filteringColumn in GetFilteringColumnFromStatement(script, statementList))
                {
                    result.Add(filteringColumn);
                }
            });
        });

        return result.ToList();
    }

    private IEnumerable<ColumnReference> GetFilteringColumnFromStatement(IScriptModel script, TSqlFragment fragment)
    {
        var finder = new FilteringColumnFinder(_issueReporter, _astService, script.ParsedScript, script.RelativeScriptFilePath, _context.DefaultSchemaName, script.ParentFragmentProvider);

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
