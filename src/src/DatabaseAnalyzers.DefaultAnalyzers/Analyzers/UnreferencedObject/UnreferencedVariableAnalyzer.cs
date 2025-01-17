using DatabaseAnalyzer.Common.Extensions;
using DatabaseAnalyzer.Contracts;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;

public sealed class UnreferencedVariableAnalyzer : IScriptAnalyzer
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
        var referencedVariableNames = batch
            .GetChildren<VariableReference>(recursive: true)
            .Select(static a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var variableDeclarations = batch
            .GetChildren<DeclareVariableElement>(recursive: true)
            .Where(static a => a is not ProcedureParameter);

        foreach (var variableDeclaration in variableDeclarations)
        {
            if (referencedVariableNames.Contains(variableDeclaration.VariableName.Value))
            {
                continue;
            }

            var fullObjectName = variableDeclaration.TryGetFirstClassObjectName(context, script);
            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtFragment(variableDeclaration) ?? DatabaseNames.Unknown;
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, variableDeclaration.GetCodeRegion(), variableDeclaration.VariableName.Value);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5012",
            IssueType.Warning,
            "Unreferenced variable",
            "The variable `{0}` is declared but not used.",
            ["Variable name"],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
