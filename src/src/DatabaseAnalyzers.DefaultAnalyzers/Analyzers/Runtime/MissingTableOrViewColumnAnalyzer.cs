using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime;

public sealed class MissingTableOrViewColumnAnalyzer : IGlobalAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics { get; } = [SharedDiagnosticDefinitions.MissingObject];

    public void Analyze(IAnalysisContext context)
    {
        var settings = context.DiagnosticSettingsProvider.GetSettings<Aj5044Settings>();
        var databasesByName = new DatabaseObjectExtractor(context.IssueReporter)
            .Extract(context.ErrorFreeScripts, context.DefaultSchemaName);

        foreach (var script in context.ErrorFreeScripts)
        {
            foreach (var columnReference in script.ParsedScript.GetChildren<ColumnReferenceExpression>(recursive: true))
            {
                AnalyzeTableReference(context, script, columnReference, databasesByName, settings);
            }
        }
    }

    private static void AnalyzeTableReference(IAnalysisContext context, IScriptModel script, ColumnReferenceExpression columnReference, IReadOnlyDictionary<string, DatabaseInformation> databasesByName, Aj5044Settings settings)
    {
        var columnResolver = new TableColumnResolver(context.IssueReporter, script.ParsedScript, script.RelativeScriptFilePath, script.ParentFragmentProvider, context.DefaultSchemaName);
        var resolvedColumn = columnResolver.Resolve(columnReference);
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

        if (IsIgnored(settings, resolvedColumn))
        {
            return;
        }

        var fullObjectName = columnReference.TryGetFirstClassObjectName(context, script);

        context.IssueReporter.Report(SharedDiagnosticDefinitions.MissingObject, resolvedColumn.DatabaseName, script.RelativeScriptFilePath, fullObjectName, columnReference.GetCodeRegion(), "column", resolvedColumn.FullName);
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

    private static bool IsIgnored(Aj5044Settings settings, ColumnReference reference)
    {
        if (settings.IgnoredObjectNamePatterns.Count == 0)
        {
            return false;
        }

        var fullObjectName = $"{reference.DatabaseName}.{reference.SchemaName}.{reference.TableName}.{reference.ColumnName}";
        return settings.IgnoredObjectNamePatterns.Any(a => a.IsMatch(fullObjectName));
    }
}
