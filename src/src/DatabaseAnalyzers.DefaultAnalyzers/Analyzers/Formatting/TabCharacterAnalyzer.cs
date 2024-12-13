using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public class TabCharacterAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, ScriptModel script)
    {
        for (var i = 0; i < script.Script.Sql.Length; i++)
        {
            var c = script.Script.Sql[i];
            if (c != '\t')
            {
                continue;
            }

            var (lineNumber, columnNumber) = script.Script.Sql.GetLineAndColumnNumber(i);

            var codeRegion = CodeRegion.Create(lineNumber, columnNumber, lineNumber, columnNumber + 1);

            var fullObjectName = script.Script
                .GetCodeObjectAtPosition(i)
                ?.TryGetFullObjectName(context.DefaultSchemaName);

            context.IssueReporter.Report(DiagnosticDefinitions.Default, script.RelativeScriptFilePath, fullObjectName, codeRegion);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5008",
            IssueType.Formatting,
            "Tab character",
            "Tab character."
        );
    }
}
