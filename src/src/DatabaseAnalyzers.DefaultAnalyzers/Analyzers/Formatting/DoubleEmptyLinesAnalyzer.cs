using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class DoubleEmptyLinesAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var whitespaceTokens = script.ParsedScript.Tokens
            .Where(a => a.IsWhiteSpace())
            .ToList();

        foreach (var token in whitespaceTokens)
        {
            if (token.Text.Count(a => a == '\n') <= 2)
            {
                continue;
            }

            var fullObjectName = script.ParsedScript
                .TryGetCodeObjectAtPosition(token.StartLocation)
                ?.TryGetFullObjectName(context.DefaultSchemaName);

            context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, token);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5007",
            IssueType.Formatting,
            "Multiple empty lines",
            "Multiple empty lines."
        );
    }
}
