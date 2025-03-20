using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.Services;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewColumnAnalyzer : IGlobalAnalyzer
{
    private readonly IAstService _astService;
    private readonly IGlobalAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5044Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingTableOrViewColumnAnalyzer(IGlobalAnalysisContext context, Aj5044Settings settings, IAstService astService, IObjectProvider objectProvider)
    {
        _context = context;
        _settings = settings;
        _astService = astService;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        foreach (var script in _context.ErrorFreeScripts)
        {
            foreach (var columnReference in script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
            {
                AnalyzeTableReference(script, columnReference);
            }
        }
    }

    private void AnalyzeTableReference(IScriptModel script, ColumnReferenceExpression columnReference)
    {
        var columnResolver = new ColumnResolver(_context.IssueReporter, _astService, script.ParsedScript, script.RelativeScriptFilePath, script.ParentFragmentProvider, _context.DefaultSchemaName);
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

        if (DoesColumnExist(resolvedColumn.DatabaseName, resolvedColumn.SchemaName, resolvedColumn.TableName, resolvedColumn.ColumnName))
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
            return view.Columns.Contains(columnName, StringComparer.OrdinalIgnoreCase);
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
