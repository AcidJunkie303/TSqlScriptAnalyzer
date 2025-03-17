using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.Services;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewAnalyzer : IGlobalAnalyzer
{
    private readonly IAstService _astService;
    private readonly IAnalysisContext _context;
    private readonly Aj5044Settings _settings;

    public MissingTableOrViewAnalyzer(IAnalysisContext context, Aj5044Settings settings, IAstService astService)
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

        foreach (var script in _context.Scripts)
        {
            foreach (var batch in script.ParsedScript.Batches)
            {
                AnalyzeBatch(script, batch, databasesByName);
            }
        }
    }

    private void AnalyzeBatch(IScriptModel script, TSqlBatch batch, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        foreach (var tableReference in batch.GetChildren<NamedTableReference>(recursive: true))
        {
            AnalyzeTableReference(script, tableReference, databasesByName);
        }
    }

    private void AnalyzeTableReference(IScriptModel script, NamedTableReference tableReference, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var tableResolver = new TableResolver(_context.IssueReporter, _astService, script.ParsedScript, tableReference, script.RelativeScriptFilePath, script.ParentFragmentProvider, _context.DefaultSchemaName);
        var resolvedTable = tableResolver.Resolve();
        if (resolvedTable is null)
        {
            return;
        }

        if (resolvedTable.SourceType != TableSourceType.TableOrView)
        {
            return;
        }

        if (DoesTableOrViewExist(resolvedTable.DatabaseName, resolvedTable.SchemaName, resolvedTable.ObjectName, databasesByName))
        {
            return;
        }

        if (IsIgnored(resolvedTable))
        {
            return;
        }

        var fullObjectName = tableReference.TryGetFirstClassObjectName(_context, script);

        _context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, resolvedTable.DatabaseName, script.RelativeScriptFilePath, fullObjectName, tableReference.GetCodeRegion(), "table or view", resolvedTable.FullName);
    }

    private static bool DoesTableOrViewExist(string databaseName, string schemaName, string tableOrViewName, IReadOnlyDictionary<string, DatabaseInformation> databasesByName)
    {
        var schema = databasesByName
            .GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);

        if (schema is null)
        {
            return false;
        }

        return schema.TablesByName.ContainsKey(tableOrViewName)
               || schema.ViewsByName.ContainsKey(tableOrViewName);
    }

    private bool IsIgnored(TableOrViewReference reference)
    {
        if (_settings.IgnoredObjectNamePatterns.Count == 0)
        {
            return false;
        }

        var fullObjectName = $"{reference.DatabaseName}.{reference.SchemaName}.{reference.ObjectName}";
        return _settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
    }
}
