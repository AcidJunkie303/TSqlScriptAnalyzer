using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Common.SqlParsing;
using DatabaseAnalyzer.Common.SqlParsing.Extraction.Models;
using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Settings;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Runtime.MissingObjectAnalyzers;

internal sealed class MissingTableOrViewAnalyzer : AnalyzerBase
{
    public MissingTableOrViewAnalyzer(IAnalysisContext context, Aj5044Settings settings, IReadOnlyDictionary<string, DatabaseInformation> databasesByName) : base(context, settings, databasesByName)
    {
    }

    public override void Analyze()
    {
        foreach (var script in Context.Scripts)
        {
            foreach (var batch in script.ParsedScript.Batches)
            {
                AnalyzeBatch(batch, script);
            }
        }
    }

    private void AnalyzeBatch(TSqlBatch batch, IScriptModel script)
    {
        foreach (var tableReference in batch.GetChildren<NamedTableReference>(recursive: true))
        {
            AnalyzeTableReference(tableReference, script);
        }
    }

    private void AnalyzeTableReference(NamedTableReference tableReference, IScriptModel script)
    {
        var tableResolver = new TableResolver(Context.IssueReporter, script.ParsedScript, script.RelativeScriptFilePath, script.ParentFragmentProvider, Context.DefaultSchemaName);
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

        var fullObjectName = tableReference.TryGetFirstClassObjectName(Context, script);

        Context.IssueReporter.Report(DiagnosticDefinitions.Default, resolvedTable.DatabaseName, script.RelativeScriptFilePath, fullObjectName, tableReference.GetCodeRegion(), "table or view", resolvedTable.FullName);
    }

    private bool DoesTableOrViewExist(string databaseName, string schemaName, string tableOrViewName)
    {
        var schema = DatabasesByName
            .GetValueOrDefault(databaseName)
            ?.SchemasByName.GetValueOrDefault(schemaName);

        if (schema is null)
        {
            return false;
        }

        return schema.TablesByName.ContainsKey(tableOrViewName)
               || schema.ViewsByName.ContainsKey(tableOrViewName);
    }
}
