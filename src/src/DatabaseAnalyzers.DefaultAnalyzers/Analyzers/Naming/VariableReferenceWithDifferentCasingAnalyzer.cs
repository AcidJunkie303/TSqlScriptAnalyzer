using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;

public sealed class VariableReferenceWithDifferentCasingAnalyzer : IScriptAnalyzer
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
        var variableDeclarationsByName = batch
            .GetDescendantsOfType<SqlVariableDeclaration>()
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => a.First())
            .ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);

        var variableReferencesWithDifferentCasing = batch.Tokens
            .Where(a => a.IsVariable())
            .Where(a => !IsParameter(a, script.ParsedScript))
            .Select(a =>
            {
                var variableName = a.Text;
                if (!variableDeclarationsByName.TryGetValue(variableName, out var variableDeclaration))
                {
                    return default;
                }

                var hasDifferentCasing = variableDeclaration.Name.EqualsOrdinalIgnoreCase(variableName)
                                         && !variableDeclaration.Name.EqualsOrdinal(variableName);
                return hasDifferentCasing
                    ? (Token: a, DeclaredName: variableDeclaration.Name)
                    : default;
            })
            .Where(a => a != default);

        foreach (var (token, declaredName) in variableReferencesWithDifferentCasing)
        {
            var fullObjectName = script.ParsedScript.TryGetFullObjectNameAtPosition(context.DefaultSchemaName, token.StartLocation);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, token, token.Text, declaredName);
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
            "AJ5014",
            IssueType.Warning,
            "Variable reference with different casing",
            "The variable reference '{0}' has different casing compared to the declaration '{1}'."
        );
    }
}
