using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Models;

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

#pragma warning disable S125
        /*
        foreach (var token in script.ParsedScript.ScriptTokenStream)
        {
            if (TokenTypesToCheck.Contains(token.TokenType))
            {
                CheckToken(token);
            }
        }

        void CheckToken(TSqlParserToken token)
        {
            var indices = token.Text
                .Select((c, i) => (IsTab: c == '\t', Index: i))
                .Where(a => a.IsTab)
                .Select(a => a.Index);

            Lazy<string?> lazyFullObjectName = new(() => script.ParsedScript.TryGetFirstClassObjectName(context, script));
            Lazy<CodeRegion> lazyCodeTokenCodeRegion = new(token.GetCodeRegion);

            foreach (var index in indices)
            {
                var columnNumber = token.Column + index;
                var codeRegion = lazyCodeTokenCodeRegion.Value with
                {
                    StartColumnNumber = columnNumber, EndColumnNumber = columnNumber
                };
                context.IssueReporter.Report(DiagnosticDefinitions.Default, script, lazyFullObjectName.Value, codeRegion);
            }
        }
*/
#pragma warning restore S125
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
