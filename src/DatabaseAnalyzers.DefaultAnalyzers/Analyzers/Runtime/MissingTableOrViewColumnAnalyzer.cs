using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.Services;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewColumnAnalyzer : IGlobalAnalyzer
{
    private readonly IAstService _astService;
    private readonly IAnalysisContext _context;
    private readonly Aj5044Settings _settings;

    public MissingTableOrViewColumnAnalyzer(IAnalysisContext context, Aj5044Settings settings, IAstService astService)
    {
        _context = context;
        _settings = settings;
        _astService = astService;
    }

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public void Analyze()
    {
        var databasesByName = new DatabaseObjectExtractor(_context.IssueReporter)
            .Extract(_context.ErrorFreeScripts, _context.DefaultSchemaName);

        foreach (var script in _context.ErrorFreeScripts)
        {
            foreach (var columnReference in script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
            {
                AnalyzeTableReference(script, columnReference, databasesByName);
            }
        }
    }

    private void AnalyzeTableReference(IScriptModel script, ColumnReferenceExpression columnReference, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var columnResolver = new TableColumnResolver(_context.IssueReporter, script.ParsedScript, columnReference, script.RelativeScriptFilePath, script.ParentFragmentProvider, _context.DefaultSchemaName);
        var resolvedColumn = columnResolver.Resolve();
        if (resolvedColumn is null)
        {
            return;
        }

        if (resolvedColumn.SourceType is TableSourceType.Cte or TableSourceType.TempTable)
        {
            return;
        }

        if (DoesColumnExist(databasesByName, resolvedColumn.DatabaseName, resolvedColumn.SchemaName, resolvedColumn.TableName, resolvedColumn.ColumnName))
        {
            return;
        }

        if (IsIgnored(resolvedColumn))
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

    private static bool DoesColumnExist(IReadOnlyDictionary<string, DatabaseInformation> databasesByName, string databaseName, string schemaName, string tableOrViewName, string columnName)
    {
        var schema = databasesByName
            .GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);

        if (schema is null)
        {
            return false;
        }

        if (schema.TablesByName.TryGetValue(tableOrViewName, out var table))
        {
            return table.Columns.Any(a => a.ObjectName.EqualsOrdinalIgnoreCase(columnName));
        }

        if (schema.ViewsByName.TryGetValue(tableOrViewName, out var view))
        {
            return view.Columns.Any(a => a.EqualsOrdinalIgnoreCase(columnName));
        }

        if (schema.SynonymsByName.TryGetValue(tableOrViewName, out var synonym))
        {
            return DoesColumnExist(databasesByName, synonym.DatabaseName, synonym.SchemaName, synonym.TargetObjectName, columnName);
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
