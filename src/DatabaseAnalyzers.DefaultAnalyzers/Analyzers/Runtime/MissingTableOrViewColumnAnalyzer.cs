using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewColumnAnalyzer : IGlobalAnalyzer
{
    private readonly IAstService _astService;
    private readonly IColumnResolverFactory _columnResolverFactory;
    private readonly IGlobalAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly ParallelOptions _parallelOptions;
    private readonly Aj5044Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingTableOrViewColumnAnalyzer(IGlobalAnalysisContext context, Aj5044Settings settings, IAstService astService, IObjectProvider objectProvider, IColumnResolverFactory columnResolverFactory, ParallelOptions parallelOptions)
    {
        _context = context;
        _settings = settings;
        _astService = astService;
        _objectProvider = objectProvider;
        _columnResolverFactory = columnResolverFactory;
        _parallelOptions = parallelOptions;
    }

    public void Analyze()
    {
        Parallel.ForEach(_context.ErrorFreeScripts, _parallelOptions, script =>
        {
            foreach (var columnReference in script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
            {
                AnalyzeTableReference(script, columnReference);
            }
        });
    }

    private void AnalyzeTableReference(IScriptModel script, ColumnReferenceExpression columnReference)
    {
        var columnResolver = _columnResolverFactory.CreateColumnResolver(_context, script);
        var resolvedColumn = columnResolver.Resolve(columnReference);
        if (resolvedColumn is null)
        {
            return;
        }

        if (resolvedColumn.TableName.IsTempTableName())
        {
            return;
        }

        if (resolvedColumn.SourceType is TableSourceType.Cte or TableSourceType.TempTable)
        {
            return;
        }

        if (IsIgnored(resolvedColumn))
        {
            return;
        }

        // we don't check the left part of the assignment clause in update statements
        if (columnReference.GetParents(script.ParentFragmentProvider).OfType<AssignmentSetClause>().Any())
        {
            return;
        }

        if (DoesColumnExist(resolvedColumn.DatabaseName, resolvedColumn.SchemaName, resolvedColumn.TableName, resolvedColumn.ColumnName))
        {
            return;
        }

        if (_astService.IsChildOfFunctionEnumParameter(resolvedColumn.Fragment, script.ParentFragmentProvider))
        {
            return;
        }

        var fullObjectName = columnReference.TryGetFirstClassObjectName(_context, script);

        _context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, resolvedColumn.DatabaseName, script.RelativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), "column", resolvedColumn.FullName);
    }

    private bool DoesColumnExist(string databaseName, string schemaName, string tableOrViewName, string columnName)
    {
        var table = _objectProvider.GetTable(databaseName, schemaName, tableOrViewName);
        if (table is not null)
        {
            return table.ColumnsByName.ContainsKey(columnName);
        }

        var view = _objectProvider.GetView(databaseName, schemaName, tableOrViewName);
        if (view is not null)
        {
            return view.ColumnsByName.ContainsKey(columnName);
        }

        var synonym = _objectProvider.GetSynonym(databaseName, schemaName, tableOrViewName);
        if (synonym is not null)
        {
            return DoesColumnExist(synonym.DatabaseName, synonym.SchemaName, synonym.TargetObjectName, columnName);
        }

        return false;
    }

    private bool IsIgnored(ColumnReference reference)
    {
        if (_settings.IgnoredObjectNamePatterns.Count == 0)
        {
            return false;
        }

        var fullObjectName = $"{reference.DatabaseName}.{reference.SchemaName}.{reference.TableName}.{reference.ColumnName}";
        return _settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
    }
}
