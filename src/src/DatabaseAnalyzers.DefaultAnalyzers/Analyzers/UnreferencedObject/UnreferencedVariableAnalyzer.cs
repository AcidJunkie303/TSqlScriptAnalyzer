using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.UnreferencedObject;

public sealed class UnreferencedVariableAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        foreach (var batch in script.ParsedScript.Batches)
        {
            AnalyzeBatch(context, script, batch);
        }
    }

    private static void AnalyzeBatch(IAnalysisContext context, ScriptModel script, SqlBatch batch)
    {
        var singleVariables = batch.Tokens
            .Where(a => a.IsVariable() && !IsParameter(a, script.ParsedScript))
            .GroupBy(a => a.Text, StringComparer.OrdinalIgnoreCase)
            .Select(a => (VariableName: a.Key, Tokens: a.ToList()))
            .Where(a => a.Tokens.Count == 1)
            .Select(a => a.Tokens[0]);

        foreach (var token in singleVariables)
        {
            var fullObjectName = script.ParsedScript.TryGetFullObjectNameAtPosition(context.DefaultSchemaName, token.StartLocation);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, token, token.Text);
        }
    }

    private static bool IsParameter(Token token, SqlScript script)
    {
        var codeObject = script.TryGetCodeObjectAtPosition(token.StartLocation);

        return codeObject is not null
               &&
               (
                   codeObject is SqlParameterDeclaration
                   || codeObject.GetParents().Any(a => a is SqlParameterDeclaration)
               );
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5012",
            IssueType.Warning,
            "Unreferenced variable",
            "The variable '{0}' is declared but not used"
        );
    }
}
