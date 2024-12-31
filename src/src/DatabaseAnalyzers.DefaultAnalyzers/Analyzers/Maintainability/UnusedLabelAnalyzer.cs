using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Maintainability;

public sealed class UnusedLabelAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var batch in script.ParsedScript.Batches)
        {
            AnalyzeBatch(context, script, batch);
        }
    }

    private static void AnalyzeBatch(IAnalysisContext context, IScriptModel script, TSqlBatch batch)
    {
        var referencedLabelNames = batch
            .GetChildren<GoToStatement>(recursive: true)
            .Select(static a => a.LabelName.Value.TrimEnd(':'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var label in batch.GetChildren<LabelStatement>(recursive: true))
        {
            var labelName = label.Value.TrimEnd(':');
            if (referencedLabelNames.Contains(labelName))
            {
                continue;
            }

            var databaseName = label.FindCurrentDatabaseNameAtFragment(script.ParsedScript);
            var fullObjectName = label.TryGetFirstClassObjectName(context, script);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, label.GetCodeRegion(), labelName);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5036",
            IssueType.Warning,
            "Unreferenced Label",
            "The label '{0}' is not referenced and can be removed."
        );
    }
}
