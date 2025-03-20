using System.Collections.Immutable;
using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Common.Various;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Indices;

public sealed class UnusedIndexAnalyzer : IGlobalAnalyzer
{
    private readonly IAstService _astService;
    private readonly IGlobalAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5051Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [DiagnosticDefinitions.Default];

    public UnusedIndexAnalyzer(IGlobalAnalysisContext context, Aj5051Settings settings, IAstService astService, IObjectProvider objectProvider)
    {
        _context = context;
        _settings = settings;
        _astService = astService;
        _objectProvider = objectProvider;
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

        foreach (var index in allIndices)
        {
            foreach (var column in index.ColumnNames)
            {
                if (_settings.IgnoreUnusedPrimaryKeyIndices && index.IndexType.HasFlag(TableColumnIndexTypes.PrimaryKey))
                {
                    continue;
                }

                var key = new Key(index.DatabaseName, index.SchemaName, index.TableName, column);
                if (filteringColumnsByName.ContainsKey(key))
                {
                    continue;
                }

                _context.IssueReporter.Report(DiagnosticDefinitions.Default, index.DatabaseName, index.RelativeScriptFilePath, index.IndexName, index.CreationStatement.GetCodeRegion(),
                    index.DatabaseName, index.SchemaName, index.TableName, column, index.IndexName ?? Constants.UnknownObjectName);
            }
        }
    }

    private IEnumerable<ColumnReference> GetFilteringColumns()
    {
        foreach (var script in _context.ErrorFreeScripts)
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

                foreach (var filteringColumn in GetFilteringColumnFromStatement(script, statementList))
                {
                    yield return filteringColumn;
                }
            }
        }
    }

    private IEnumerable<ColumnReference> GetFilteringColumnFromStatement(IScriptModel script, TSqlFragment fragment)
    {
        var finder = new FilteringColumnFinder(_context.IssueReporter, _astService, script.ParsedScript, script.RelativeScriptFilePath, _context.DefaultSchemaName, script.ParentFragmentProvider);

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
