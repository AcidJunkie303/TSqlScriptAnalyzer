using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzers.DefaultAnalyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewAnalyzer : IGlobalAnalyzer
{
    private readonly IGlobalAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly ParallelOptions _parallelOptions;
    private readonly Aj5044Settings _settings;
    private readonly ITableResolverFactory _tableResolverFactory;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingTableOrViewAnalyzer(IGlobalAnalysisContext context, Aj5044Settings settings, IObjectProvider objectProvider, ParallelOptions parallelOptions, ITableResolverFactory tableResolverFactory)
    {
        _context = context;
        _settings = settings;
        _objectProvider = objectProvider;
        _parallelOptions = parallelOptions;
        _tableResolverFactory = tableResolverFactory;
    }

    public void Analyze()
    {
        // pretty performance hungry
        Parallel.ForEach(_context.Scripts, _parallelOptions, script =>
        {
            foreach (var batch in script.ParsedScript.Batches)
            {
                AnalyzeBatch(script, batch);
            }
        });
    }

    private void AnalyzeBatch(IScriptModel script, TSqlBatch batch)
    {
        foreach (var tableReference in batch.GetChildren<NamedTableReference>(recursive: true))
        {
            AnalyzeTableReference(script, tableReference);
        }
    }

    private void AnalyzeTableReference(IScriptModel script, NamedTableReference tableReference)
    {
        var tableResolver = _tableResolverFactory.CreateTableResolver(_context, script);
        var resolvedTable = tableResolver.Resolve(tableReference);
        if (resolvedTable is null)
        {
            return;
        }

        if (resolvedTable.SourceType != TableSourceType.TableOrView)
        {
            return;
        }

        if (DoesTableOrViewExist(resolvedTable.DatabaseName, resolvedTable.SchemaName, resolvedTable.ObjectName))
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

    private bool DoesTableOrViewExist(string databaseName, string schemaName, string tableOrViewName)
    {
        return _objectProvider.GetTable(databaseName, schemaName, tableOrViewName) is not null
               || _objectProvider.GetView(databaseName, schemaName, tableOrViewName) is not null;
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
