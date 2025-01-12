using DatabaseAnalyzer.Contracts;
using DatabaseAnalyzer.Contracts.DefaultImplementations.Extensions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Formatting;

public sealed class DoubleEmptyLinesAnalyzer : IScriptAnalyzer
{
    public IReadOnlyList<IDiagnosticDefinition> SupportedDiagnostics => [DiagnosticDefinitions.Default];

    public void AnalyzeScript(IAnalysisContext context, IScriptModel script)
    {
        foreach (var block in GetConsecutiveBlankSpaceTokens(script.ParsedScript.ScriptTokenStream))
        {
            var newLineCharCount = block.Sum(static a => a.Text.Count(static x => x == '\n'));
            if (newLineCharCount <= 2)
            {
                continue;
            }

            var firstToken = block[0];
            var lastToken = block[^1];
            int endLine;
            int endColumn;

            if (lastToken.Text[^1] == '\n')
            {
                endLine = lastToken.Line + 1;
                endColumn = 1;
            }
            else
            {
                endLine = lastToken.Line;
                endColumn = lastToken.Column;
            }

            var codeRegion = CodeRegion.Create(firstToken.Line, firstToken.Column, endLine, endColumn);
            var fullObjectName = script.ParsedScript
                .TryGetSqlFragmentAtPosition(block[0])
                ?.TryGetFirstClassObjectName(context, script);

            var databaseName = script.ParsedScript.TryFindCurrentDatabaseNameAtLocation(block[0].Line, block[0].Column) ?? DatabaseNames.Unknown;
            context.IssueReporter.Report(DiagnosticDefinitions.Default, databaseName, script.RelativeScriptFilePath, fullObjectName, codeRegion);
        }
    }

    private static IEnumerable<List<TSqlParserToken>> GetConsecutiveBlankSpaceTokens(IList<TSqlParserToken> tokens)
    {
        List<TSqlParserToken>? currentGroup = null;

        foreach (var token in tokens)
        {
            if (token.TokenType == TSqlTokenType.WhiteSpace)
            {
                currentGroup ??= [];
                currentGroup.Add(token);
            }
            else if (currentGroup is not null)
            {
                yield return currentGroup;
                currentGroup = null;
            }
        }

        if (currentGroup is not null)
        {
            yield return currentGroup;
        }
    }

    private static class DiagnosticDefinitions
    {
        public static DiagnosticDefinition Default { get; } = new
        (
            "AJ5007",
            IssueType.Formatting,
            "Multiple empty lines",
            "Multiple empty lines.",
            [],
            new Uri("https://github.com/AcidJunkie303/TSqlScriptAnalyzer/blob/main/docs/diagnostics/{DiagnosticId}.md")
        );
    }
}
