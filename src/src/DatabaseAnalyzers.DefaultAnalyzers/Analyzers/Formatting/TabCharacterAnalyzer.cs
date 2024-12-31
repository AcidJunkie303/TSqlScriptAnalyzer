using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class TabCharacterAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var sqlCode = script.ParsedScript.GetSql();
        for (var i = 0; i < sqlCode.Length; i++)
        {
            var c = sqlCode[i];
            if (c != '\t')
            {
                continue;
            }

            var (lineNumber, columnNumber) = sqlCode.GetLineAndColumnNumber(i);

            var codeRegion = CodeRegion.Create(lineNumber, columnNumber, lineNumber, columnNumber + 1);

            var fullObjectName = script.ParsedScript
                .TryGetSqlFragmentAtPosition(i)
                ?.TryGetFirstClassObjectName(context, script);

            context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, codeRegion);
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
