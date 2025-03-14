using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5044Settings>();
        var databasesByName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.ErrorFreeScripts, context.DefaultSchemaName);

        foreach (var script in context.Scripts)
        {
            foreach (var batch in script.ParsedScript.Batches)
            {
                AnalyzeBatch(context, script, batch, databasesByName, settings);
            }
        }
    }

    private static void AnalyzeBatch(IAnalysisContext context, IScriptModel script, TSqlBatch batch, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, Aj5044Settings settings)
    {
        foreach (var tableReference in batch.GetChildren<NamedTableReference>(recursive: true))
        {
            AnalyzeTableReference(context, script, tableReference, databasesByName, settings);
        }
    }

    private static void AnalyzeTableReference(IAnalysisContext context, IScriptModel script, NamedTableReference tableReference, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, Aj5044Settings settings)
    {
        var tableResolver = new TableResolver(context.IssueReporter, script.ParsedScript, tableReference, script.RelativeScriptFilePath, script.ParentFragmentProvider, context.DefaultSchemaName);
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

        if (IsIgnored(settings, resolvedTable))
        {
            return;
        }

        var fullObjectName = tableReference.TryGetFirstClassObjectName(context, script);

        context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, resolvedTable.DatabaseName, script.RelativeScriptFilePath, fullObjectName, tableReference.GetCodeRegion(), "table or view", resolvedTable.FullName);
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

    private static bool IsIgnored(Aj5044Settings settings, TableOrViewReference reference)
    {
        if (settings.IgnoredObjectNamePatterns.Count == 0)
        {
            return false;
        }

        var fullObjectName = $"{reference.DatabaseName}.{reference.SchemaName}.{reference.ObjectName}";
        return settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
    }
}
