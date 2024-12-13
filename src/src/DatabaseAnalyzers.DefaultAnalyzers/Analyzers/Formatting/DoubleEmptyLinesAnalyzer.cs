using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public class DoubleEmptyLinesAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        var whitespaceTokens = script.Script.Tokens
            .Where(a => a.IsWhiteSpace())
            .ToList();

        for (var i = 0; i < whitespaceTokens.Count; i++)
        {
            var token = whitespaceTokens[i];

            if (token.Text.Count(a => a == '\n') > 2)
            {
                var fullObjectName = script.Script
                    .GetCodeObjectAtPosition(token.StartLocation)
                    ?.TryGetFullObjectName(context.DefaultSchemaName);

                context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, token);
            }
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
