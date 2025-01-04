using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class VariableReferenceWithDifferentCasingAnalyzer : IScriptAnalyzer
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
        var variableDeclarationsByName = batch
            .GetChildren<DeclareVariableStatement>(recursive: true)
            .SelectMany(static a => a.Declarations)
            .GroupBy(static a => a.VariableName.Value, StringComparer.OrdinalIgnoreCase)
            .Select(static a => a.First())
            .ToDictionary(static a => a.VariableName.Value, static a => a, StringComparer.OrdinalIgnoreCase);

        foreach (var variableReference in batch.GetChildren<VariableReference>(recursive: true))
        {
            var variableReferenceName = variableReference.Name;

            // case-insensitive lookup
            if (!variableDeclarationsByName.TryGetValue(variableReferenceName, out var variableDeclaration))
            {
                continue;
            }

            if (variableReferenceName.EqualsOrdinal(variableDeclaration.VariableName.Value))
            {
                continue;
            }

            var fullObjectName = variableReference.TryGetFirstClassObjectName(context, script);
            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(variableReference) ?? DatabaseNames.Unknown;
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, variableReference.GetCodeRegion(), variableReference.Name, variableDeclaration.VariableName.Value);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5014",
            IssueType.Warning,
            "Variable reference with different casing",
            "The variable reference '{0}' has different casing compared to the declaration '{1}'.",
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
