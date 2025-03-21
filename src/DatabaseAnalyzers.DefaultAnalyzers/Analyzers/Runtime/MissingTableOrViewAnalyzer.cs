using DatabaseAnalyzer.Common.Contracts;
using DatabaseAnalyzer.Common.Contracts.Services;
using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewAnalyzer : IGlobalAnalyzer
{
    private readonly IAstService _astService;
    private readonly IGlobalAnalysisContext _context;
    private readonly IObjectProvider _objectProvider;
    private readonly Aj5044Settings _settings;

    public static IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public MissingTableOrViewAnalyzer(IGlobalAnalysisContext context, Aj5044Settings settings, IAstService astService, IObjectProvider objectProvider)
    {
        _context = context;
        _settings = settings;
        _astService = astService;
        _objectProvider = objectProvider;
    }

    public void Analyze()
    {
        Parallel.ForEach(_context.Scripts, script =>
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
        var tableResolver = new TableResolverOld(_context.IssueReporter, _astService, script.ParsedScript, tableReference, script.RelativeScriptFilePath, script.ParentFragmentProvider, _context.DefaultSchemaName);
        var resolvedTable = tableResolver.Resolve();
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
