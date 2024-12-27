using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Collaboration;

public sealed class TabulatorCharacterAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        var sql = script.ParsedScript.GetSql();

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            if (c != '\t')
            {
                continue;
            }

            var fullObjectName = script.ParsedScript
                .TryGetSqlFragmentAtPosition(i)
                ?.TryGetFirstClassObjectName(context, script);
            var (lineNumber, columnNumber) = sql.GetLineAndColumnNumber(i);
            var codeRegion = CodeRegion.Create(lineNumber, columnNumber, lineNumber, columnNumber + 1);
            context.IssueReporter.Report(DiagnosticDefinitions.Default, script, fullObjectName, codeRegion);
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5019",
            IssueType.Formatting,
            "Tabulator character",
            "For better collaboration with other team members, do not use tabulator characters. Instead, replace them with spaces."
        );
    }
}
